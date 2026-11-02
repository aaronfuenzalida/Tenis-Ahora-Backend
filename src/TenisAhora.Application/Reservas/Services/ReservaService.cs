using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Pagos.Interfaces;
using TenisAhora.Application.Reservas.Dtos;
using TenisAhora.Application.Reservas.Interfaces;
using TenisAhora.Domain.Common;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Reservas.Services;

public class ReservaService : ServicioCrud<Reserva>, IReservaService
{
    // Cada alquiler dura como máximo dos horas (consigna).
    private const decimal MaximoHorasAlquiler = 2;

    private static readonly (string Categoria, string Etiqueta)[] Equipos = [("Redes", "redes"), ("Pelotas", "pelotas"), ("Raquetas", "raquetas")];

    // El estado, los cobros y los costos solo cambian por los endpoints de negocio, no por el PUT.
    private static readonly string[] CamposDeSoloLectura =
    [
        "status", "depositAmount", "depositPaid", "depositPaymentMethod", "depositTransactionId", "depositReceiptNumber",
        "remainingPaid", "finalReceiptNumber", "finalPaymentMethod", "finalTransactionId", "cancellationDetails",
        "equipmentAssigned", "equipmentTaken", "courtName", "surfaceType", "courtCost", "equipmentCost", "totalCost",
        "remainingBalance", "endTime", "createdAt", "confirmedAt", "startedAt", "finishedAt",
    ];

    private readonly IRepositorio<Cancha> _canchaRepository;
    private readonly IRepositorio<ItemStock> _stockRepository;
    private readonly IRepositorio<Recibo> _reciboRepository;
    private readonly IRepositorio<Reembolso> _reembolsoRepository;
    private readonly IPagoService _pagoService;

    public ReservaService(
        IRepositorio<Reserva> repositorio,
        IRepositorio<Cancha> canchaRepository,
        IRepositorio<ItemStock> stockRepository,
        IRepositorio<Recibo> reciboRepository,
        IRepositorio<Reembolso> reembolsoRepository,
        IPagoService pagoService) : base(repositorio)
    {
        _canchaRepository = canchaRepository;
        _stockRepository = stockRepository;
        _reciboRepository = reciboRepository;
        _reembolsoRepository = reembolsoRepository;
        _pagoService = pagoService;
    }

    protected override string Coleccion => "reservas";

    // El personal ve todas; un socio, solo las suyas.
    protected override void AutorizarListado(UsuarioActual usuario) => usuario.ExigirSocioOPersonal();

    protected override object? Visible(UsuarioActual usuario, Reserva reserva) =>
        usuario.EsPersonal || (usuario.EsSocio && EsDelTitular(usuario, reserva)) ? reserva : null;

    // Turnos tomados de todas las reservas activas, para que un socio vea qué horarios están libres.
    public async Task<List<OcupacionDto>> ListarOcupacionAsync(UsuarioActual usuario)
    {
        usuario.ExigirSocioOPersonal();
        var reservas = await _repositorio.ListarAsync();
        return reservas
            .Where(r => r.Estado != Reserva.EstadoCancelada)
            .Select(r => new OcupacionDto(r.Id, r.CanchaId, r.NombreCancha, r.Fecha, r.HoraInicio, r.HoraFin, r.DuracionHoras, r.Estado))
            .ToList();
    }

    // La reserva queda pendiente de pago: se confirma recién cuando se abona la seña del 50 % (RD01).
    public async Task<ReservaCreadaDto> CrearAsync(CrearReservaDto dto, UsuarioActual usuario)
    {
        var participantes = dto.Participants ?? [];
        if (!usuario.EsPersonal)
        {
            if (!usuario.EsSocio)
                throw AccesoDenegadoException.SoloPersonal();
            if (participantes.Count == 0 || !usuario.EsSuDni(participantes[0].Dni))
                throw new AccesoDenegadoException("Solo podés reservar a tu nombre: el titular tiene que ser tu DNI.");
        }

        var reserva = new Reserva
        {
            CanchaId = dto.CourtId ?? string.Empty,
            Fecha = dto.Date ?? string.Empty,
            HoraInicio = dto.StartTime ?? string.Empty,
            DuracionHoras = dto.DurationHours ?? 0,
            Modalidad = dto.PlayersType ?? string.Empty,
            Participantes = participantes,
            Notas = dto.Notes ?? string.Empty
        };
        VerificarHorario(reserva.Fecha, reserva.HoraInicio, reserva.DuracionHoras);
        reserva.HoraFin = Horario.Sumar(reserva.HoraInicio, reserva.DuracionHoras);
        var cancha = await VerificarCanchaAsync(reserva);
        VerificarJugadores(reserva, cancha);
        await VerificarSuperposicionAsync(reserva, excluirId: null);
        reserva.Equipamiento = EquipamientoAsignado.Para(reserva.Modalidad);
        await VerificarEquipamientoAsync(reserva.Equipamiento);

        var cantidad = await _repositorio.ContarAsync();
        reserva.Id = await _repositorio.IdLibreAsync($"RES-{DateTime.Now.Year}-{cantidad + 1:000}");
        reserva.NombreCancha = $"{cancha.Nombre} ({cancha.TipoSuperficie})";
        reserva.TipoSuperficie = cancha.TipoSuperficie;
        reserva.Estado = Reserva.EstadoPendientePago;
        reserva.CreadaEn = Formato.Iso();
        reserva.CalcularCostos(cancha.PrecioPorHora);

        await _repositorio.AgregarAsync(reserva);
        return new ReservaCreadaDto(reserva);
    }

    protected override void AutorizarCambio(UsuarioActual usuario, Reserva actual, JsonObject cambios)
    {
        usuario.ExigirPersonal();
        foreach (var campo in CamposDeSoloLectura)
            cambios.Remove(campo);
    }

    // Cambio de horario, cancha o jugadores antes de que se juegue, sujeto a disponibilidad.
    protected override async Task PrepararCambioAsync(Reserva actual, Reserva nueva, JsonObject cambios, UsuarioActual usuario)
    {
        if (actual.Estado != Reserva.EstadoPendientePago && actual.Estado != Reserva.EstadoConfirmada)
            throw new ConflictoException("Solo se modifican reservas pendientes de pago o confirmadas.");

        var cambioHorario = nueva.Fecha != actual.Fecha || nueva.HoraInicio != actual.HoraInicio || nueva.DuracionHoras != actual.DuracionHoras;
        var cambioCancha = nueva.CanchaId != actual.CanchaId;
        var cambioJugadores = nueva.Modalidad != actual.Modalidad
            || JsonSerializer.Serialize(nueva.Participantes) != JsonSerializer.Serialize(actual.Participantes);

        if (cambioHorario)
        {
            VerificarHorario(nueva.Fecha, nueva.HoraInicio, nueva.DuracionHoras);
            nueva.HoraFin = Horario.Sumar(nueva.HoraInicio, nueva.DuracionHoras);
        }
        var cancha = await _canchaRepository.ObtenerAsync(nueva.CanchaId)
            ?? throw new DatosInvalidosException($"La cancha {nueva.CanchaId} no existe.");
        if (cambioHorario || cambioCancha)
            await VerificarCanchaAsync(nueva);
        if (cambioJugadores || cambioCancha)
        {
            VerificarJugadores(nueva, cancha);
            nueva.Equipamiento = EquipamientoAsignado.Para(nueva.Modalidad);
        }
        if (cambioHorario || cambioCancha || cambioJugadores)
            await VerificarSuperposicionAsync(nueva, actual.Id);
        if (cambioCancha)
        {
            nueva.NombreCancha = $"{cancha.Nombre} ({cancha.TipoSuperficie})";
            nueva.TipoSuperficie = cancha.TipoSuperficie;
        }
        if (cambioCancha || cambioHorario)
            nueva.CalcularCostos(cancha.PrecioPorHora);
        nueva.ActualizadaEn = Formato.Iso();
    }

    // El equipamiento de un alquiler en juego vuelve al stock al cobrar el saldo.
    protected override Task VerificarBajaAsync(Reserva reserva)
    {
        if (reserva.Estado == Reserva.EstadoEnJuego)
            throw new ConflictoException("No se puede eliminar una reserva en juego: primero hay que finalizar el alquiler.");
        return Task.CompletedTask;
    }

    // Cobra la seña del 50 % con un pago aprobado, emite el recibo y confirma la reserva (RD01).
    public async Task<CobroReservaDto> PagarSenaAsync(string id, string? transaccionId, UsuarioActual usuario)
    {
        var reserva = await ObtenerExistenteAsync(id);
        ExigirTitular(usuario, reserva);
        switch (reserva.Estado)
        {
            case Reserva.EstadoPendientePago:
                break;
            case Reserva.EstadoCancelada:
                throw new ConflictoException("La reserva está cancelada.");
            default:
                throw new ConflictoException("La seña de esta reserva ya fue abonada.");
        }
        await VerificarEquipamientoAsync(reserva.Equipamiento);

        var sena = reserva.MontoSena;
        var pago = await _pagoService.ConsumirAsync(usuario, transaccionId, sena, $"reservas/{id}");
        var recibo = await EmitirReciboAsync(reserva, pago, $"Seña 50 % - {reserva.NombreCancha}", "Cobro digital online",
        [
            new ItemRecibo { Descripcion = DescripcionAlquiler(reserva), Monto = reserva.CostoCancha },
            new ItemRecibo { Descripcion = "Equipamiento incluido (red, pelotas y raquetas)", Monto = 0 },
            new ItemRecibo { Descripcion = "Total del turno", Monto = reserva.CostoTotal },
            new ItemRecibo { Descripcion = "Seña 50 % para confirmar la reserva", Monto = sena },
        ], sena);

        reserva.Estado = Reserva.EstadoConfirmada;
        reserva.SenaPagada = sena;
        reserva.MedioPagoSena = pago.MedioPago;
        reserva.TransaccionSena = pago.Id;
        reserva.ReciboSena = recibo.Id;
        reserva.ConfirmadaEn = Formato.Iso();
        await _repositorio.ActualizarAsync(reserva);
        return new CobroReservaDto(reserva, recibo);
    }

    // Registra el inicio del alquiler y descuenta del stock el equipamiento asignado (TC_002). Sin
    // stock no se inicia (TC_001).
    public async Task<Reserva> IniciarAsync(string id)
    {
        var reserva = await ObtenerExistenteAsync(id);
        switch (reserva.Estado)
        {
            case Reserva.EstadoConfirmada:
                break;
            case Reserva.EstadoPendientePago:
                throw new ConflictoException("La reserva no tiene la seña abonada: no está confirmada (RD01).");
            default:
                throw new ConflictoException("Solo se inicia el alquiler de una reserva confirmada.");
        }
        var cancha = await _canchaRepository.ObtenerAsync(reserva.CanchaId);
        if (cancha?.Estado == "mantenimiento")
            throw new ConflictoException("La cancha está bloqueada por mantenimiento (RD06).");

        reserva.EquipamientoTomado = await TomarEquipamientoAsync(reserva.Equipamiento);
        reserva.Estado = Reserva.EstadoEnJuego;
        reserva.IniciadaEn = Formato.Iso();
        await _repositorio.ActualizarAsync(reserva);
        return reserva;
    }

    // Al terminar el partido: cobra el 50 % restante, emite el recibo final, devuelve el equipamiento
    // al stock y da por finalizado el alquiler (TC_002).
    public async Task<CobroReservaDto> PagarSaldoAsync(string id, string? transaccionId, UsuarioActual usuario)
    {
        var reserva = await ObtenerExistenteAsync(id);
        switch (reserva.Estado)
        {
            case Reserva.EstadoEnJuego:
                break;
            case Reserva.EstadoFinalizada:
                throw new ConflictoException("El saldo de esta reserva ya fue liquidado.");
            case Reserva.EstadoCancelada:
                throw new ConflictoException("La reserva está cancelada.");
            default:
                throw new ConflictoException("Primero hay que registrar el inicio del alquiler.");
        }

        var saldo = reserva.SaldoRestante;
        var pago = await _pagoService.ConsumirAsync(usuario, transaccionId, saldo, $"reservas/{id}");
        await DevolverEquipamientoAsync(reserva.EquipamientoTomado);
        var recibo = await EmitirReciboAsync(reserva, pago, $"Saldo 50 % - {reserva.NombreCancha}", "Recepción del club",
        [
            new ItemRecibo { Descripcion = DescripcionAlquiler(reserva), Monto = reserva.CostoTotal },
            new ItemRecibo { Descripcion = $"Seña abonada (recibo {reserva.ReciboSena})", Monto = reserva.SenaPagada },
            new ItemRecibo { Descripcion = "Saldo restante al finalizar el partido", Monto = saldo },
        ], saldo);

        reserva.Estado = Reserva.EstadoFinalizada;
        reserva.SaldoPagado = true;
        reserva.MedioPagoFinal = pago.MedioPago;
        reserva.TransaccionFinal = pago.Id;
        reserva.ReciboFinal = recibo.Id;
        reserva.FinalizadaEn = Formato.Iso();
        await _repositorio.ActualizarAsync(reserva);
        return new CobroReservaDto(reserva, recibo);
    }

    // RD02-RD04: con la antelación mínima del club (o más) corresponde reembolso total; con menos se
    // retiene el porcentaje configurado de la seña y se reintegra el resto. Las horas se calculan contra
    // el inicio de la reserva, no las informa quien cancela. Una reserva pendiente de pago se anula sin
    // cargo porque no se abonó nada.
    public async Task<CancelacionReservaDto> CancelarAsync(string id, UsuarioActual usuario)
    {
        var reserva = await ObtenerExistenteAsync(id);
        ExigirTitular(usuario, reserva);
        var estado = reserva.Estado;
        if (estado == Reserva.EstadoCancelada)
            throw new ConflictoException("La reserva ya está cancelada.");
        if (estado != Reserva.EstadoPendientePago && estado != Reserva.EstadoConfirmada)
            throw new ConflictoException("No se puede cancelar una reserva en juego o finalizada.");

        var horas = HorasHastaElInicio(reserva);
        if (estado == Reserva.EstadoPendientePago)
        {
            reserva.Estado = Reserva.EstadoCancelada;
            reserva.Cancelacion = new DetalleCancelacion
            {
                CanceladaEn = Formato.Iso(),
                HorasDeAnticipacion = horas,
                PoliticaAplicada = "Reserva sin seña abonada: se anula sin cargo ni reembolso."
            };
            await _repositorio.ActualizarAsync(reserva);
            return new CancelacionReservaDto(reserva, true, null, null);
        }

        var horasSinCargo = ConfiguracionClub.Vigente.HorasCancelacionSinCargo;
        var retencion = ConfiguracionClub.Vigente.PorcentajeRetencion;
        var enPlazo = horas >= horasSinCargo;
        reserva.Estado = Reserva.EstadoCancelada;
        reserva.Cancelacion = new DetalleCancelacion
        {
            CanceladaEn = Formato.Iso(),
            HorasDeAnticipacion = horas,
            PoliticaAplicada = enPlazo
                ? $"Cancelación anticipada (>={Formato.Numero(horasSinCargo)}hs): Reembolso de seña según política."
                : $"Cancelación tardía (<{Formato.Numero(horasSinCargo)}hs): Se aplica retención del {Formato.Numero(retencion)}% de seña por costo operativo."
        };
        await _repositorio.ActualizarAsync(reserva);

        var (nombre, dni) = Titular(reserva);
        var sena = reserva.SenaPagada;
        var reembolso = new Reembolso
        {
            Id = await GeneradorIds.NuevoIdReembolsoAsync(_reembolsoRepository),
            NombreCliente = nombre,
            DniCliente = dni,
            ReservaRelacionada = id,
            ReciboRelacionado = reserva.ReciboSena,
            MontoOriginal = sena,
            Fecha = Formato.MarcaHoraria(),
            ProcesadoPor = "Sistema de Reservas"
        };

        Recibo? cargo = null;
        if (enPlazo)
        {
            reembolso.Motivo = $"Cancelación de reserva {id} con {Formato.Numero(horas)} hs de antelación (dentro del plazo reglamentario).";
            reembolso.Tipo = "total";
            reembolso.MontoReembolsado = sena;
            reembolso.MedioReembolso = "Devolución a " + Formato.SiVacio(reserva.MedioPagoSena, "medio de pago original");
        }
        else
        {
            var retenido = Math.Round(sena * retencion / 100, MidpointRounding.AwayFromZero);
            cargo = new Recibo
            {
                Id = await GeneradorIds.NuevoIdReciboAsync(_reciboRepository),
                ReservaId = id,
                NombreCliente = nombre,
                DniCliente = dni,
                Concepto = $"Cargo por cancelación fuera de plazo - {reserva.NombreCancha}",
                Items =
                [
                    new ItemRecibo { Descripcion = $"Seña abonada ({id})", Monto = sena },
                    new ItemRecibo { Descripcion = $"Retención {Formato.Numero(retencion)}% por cancelación con menos de {Formato.Numero(horasSinCargo)} hs", Monto = retenido },
                    new ItemRecibo { Descripcion = "Saldo reintegrado como crédito en cuenta", Monto = sena - retenido },
                ],
                TotalPagado = retenido,
                MedioPago = "Retención sobre seña",
                Estado = "completado",
                Fecha = Formato.MarcaHoraria(),
                Cajero = "Sistema de Reservas"
            };
            await _reciboRepository.AgregarAsync(cargo);
            reembolso.Motivo = $"Cancelación fuera de plazo ({Formato.Numero(horas)} hs). Se retiene el {Formato.Numero(retencion)}% de la seña y se reintegra el resto.";
            reembolso.Tipo = "parcial";
            reembolso.MontoReembolsado = sena - retenido;
            reembolso.MedioReembolso = "Crédito en cuenta de socio";
        }
        await _reembolsoRepository.AgregarAsync(reembolso);
        return new CancelacionReservaDto(reserva, enPlazo, reembolso, cargo);
    }

    // El titular de una reserva es el primer participante.
    private static bool EsDelTitular(UsuarioActual usuario, Reserva reserva) =>
        reserva.Participantes is { Count: > 0 } && usuario.EsSuDni(reserva.Participantes[0].Dni);

    private static void ExigirTitular(UsuarioActual usuario, Reserva reserva)
    {
        if (usuario.EsPersonal || (usuario.EsSocio && EsDelTitular(usuario, reserva)))
            return;
        throw new AccesoDenegadoException("La reserva no es tuya.");
    }

    // Fecha y horario válidos, no en el pasado, dentro de la antelación máxima del club (RD05) y de
    // hasta dos horas.
    private static void VerificarHorario(string fecha, string horaInicio, decimal duracionHoras)
    {
        if (!DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
            throw new DatosInvalidosException("La fecha de la reserva no es válida (formato AAAA-MM-DD).");
        if (!DateTime.TryParseExact(horaInicio, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var hora))
            throw new DatosInvalidosException("El horario de inicio no es válido (formato HH:MM).");
        if (duracionHoras <= 0 || duracionHoras > MaximoHorasAlquiler)
            throw new DatosInvalidosException($"La duración de un alquiler es de hasta {Formato.Numero(MaximoHorasAlquiler)} horas.");

        var ahora = DateTime.Now;
        if (dia < ahora.Date)
            throw new DatosInvalidosException("La fecha de la reserva ya pasó.");
        if (dia.Add(hora.TimeOfDay) <= ahora)
            throw new DatosInvalidosException("El horario de la reserva ya pasó.");

        var maximo = ConfiguracionClub.Vigente.DiasMaximosDeAnticipacion;
        var limite = ahora.Date.AddDays(maximo);
        if (dia > limite)
            throw new DatosInvalidosException($"Las reservas se pueden hacer con hasta {maximo} días de antelación (hasta el {limite:yyyy-MM-dd}) (RD05).");
    }

    // La cancha existe, no está en mantenimiento y atiende en ese turno (RD06).
    private async Task<Cancha> VerificarCanchaAsync(Reserva reserva)
    {
        if (reserva.CanchaId == string.Empty)
            throw new DatosInvalidosException("Falta elegir la cancha.");
        var cancha = await _canchaRepository.ObtenerAsync(reserva.CanchaId)
            ?? throw new DatosInvalidosException($"La cancha {reserva.CanchaId} no existe.");
        if (cancha.Estado == "mantenimiento")
            throw new ConflictoException("La cancha está bloqueada por mantenimiento y no admite reservas (RD06).");
        var motivo = cancha.MotivoDeBloqueo(reserva.Fecha, reserva.HoraInicio, reserva.DuracionHoras);
        if (motivo != null)
            throw new ConflictoException($"La cancha no admite reservas en ese turno: {motivo} (RD06).");
        return cancha;
    }

    // Los datos de los 2 (singles) o 4 (dobles) jugadores, sin repetir DNI y sin superar la capacidad
    // de la cancha.
    private static void VerificarJugadores(Reserva reserva, Cancha cancha)
    {
        var esperados = reserva.Modalidad switch
        {
            "singles" => 2,
            "dobles" => 4,
            _ => throw new DatosInvalidosException("La modalidad debe ser singles (2 jugadores) o dobles (4 jugadores).")
        };
        var participantes = reserva.Participantes ?? [];
        if (participantes.Count != esperados)
            throw new DatosInvalidosException($"Una reserva de {reserva.Modalidad} lleva los datos de {esperados} jugadores (se cargaron {participantes.Count}).");

        var vistos = new HashSet<string>();
        for (var i = 0; i < participantes.Count; i++)
        {
            var dni = Formato.SoloDigitos(participantes[i].Dni);
            if (string.IsNullOrWhiteSpace(participantes[i].Nombre) || dni == string.Empty)
                throw new DatosInvalidosException($"Falta el nombre o el DNI del jugador {i + 1}.");
            if (!vistos.Add(dni))
                throw new DatosInvalidosException($"El DNI {participantes[i].Dni} está repetido entre los jugadores.");
        }
        if (cancha.Capacidad > 0 && esperados > cancha.Capacidad)
            throw new DatosInvalidosException($"{cancha.Nombre} admite hasta {cancha.Capacidad} jugadores.");
    }

    // Ni la misma cancha en una franja superpuesta ni un jugador que ya tiene otra reserva a esa hora.
    private async Task VerificarSuperposicionAsync(Reserva reserva, string? excluirId)
    {
        var desde = Horario.AMinutos(reserva.HoraInicio);
        var hasta = Horario.AMinutos(reserva.HoraFin);
        var dnis = (reserva.Participantes ?? [])
            .Select(p => Formato.SoloDigitos(p.Dni))
            .Where(dni => dni != string.Empty)
            .ToHashSet();

        foreach (var otra in await _repositorio.ListarAsync())
        {
            if (otra.Id == excluirId || otra.Estado == Reserva.EstadoCancelada || otra.Fecha != reserva.Fecha)
                continue;
            if (!(Horario.AMinutos(otra.HoraInicio) < hasta && desde < Horario.AMinutos(otra.HoraFin)))
                continue;

            var cuando = $"el {otra.Fecha} de {otra.HoraInicio} a {otra.HoraFin}";
            if (otra.CanchaId == reserva.CanchaId)
                throw new ConflictoException($"{otra.NombreCancha} ya está reservada {cuando}.");
            var jugador = (otra.Participantes ?? []).FirstOrDefault(p => dnis.Contains(Formato.SoloDigitos(p.Dni)));
            if (jugador != null)
                throw new ConflictoException($"{jugador.Nombre} ya tiene una reserva {cuando} en {otra.NombreCancha}.");
        }
    }

    // No se alquila una cancha si no hay red, pelotas y raquetas disponibles en stock (TC_001).
    private async Task VerificarEquipamientoAsync(EquipamientoAsignado? equipamiento)
    {
        equipamiento ??= new EquipamientoAsignado();
        var items = await _stockRepository.ListarAsync();
        var faltantes = new List<string>();
        foreach (var (categoria, etiqueta) in Equipos)
        {
            var necesita = equipamiento.CantidadDe(categoria);
            var hay = items
                .Where(i => string.Equals(i.Categoria, categoria, StringComparison.OrdinalIgnoreCase))
                .Sum(i => i.Disponible);
            if (hay < necesita)
                faltantes.Add($"{etiqueta} (hay {hay}, se necesitan {necesita})");
        }
        if (faltantes.Count > 0)
            throw new ConflictoException("No hay stock suficiente para el alquiler: faltan " + string.Join(", ", faltantes) + ".");
    }

    private async Task<List<EquipamientoTomado>> TomarEquipamientoAsync(EquipamientoAsignado? equipamiento)
    {
        await VerificarEquipamientoAsync(equipamiento);
        equipamiento ??= new EquipamientoAsignado();
        var items = await _stockRepository.ListarAsync();
        var tomado = new List<EquipamientoTomado>();
        foreach (var (categoria, _) in Equipos)
        {
            var necesita = equipamiento.CantidadDe(categoria);
            foreach (var item in items)
            {
                if (necesita <= 0 || !string.Equals(item.Categoria, categoria, StringComparison.OrdinalIgnoreCase) || item.Disponible <= 0)
                    continue;
                var cantidad = Math.Min(necesita, item.Disponible);
                item.FijarEnUso(item.EnUso + cantidad);
                await _stockRepository.ActualizarAsync(item);
                tomado.Add(new EquipamientoTomado { ItemId = item.Id, Categoria = categoria, Cantidad = cantidad });
                necesita -= cantidad;
            }
        }
        return tomado;
    }

    private async Task DevolverEquipamientoAsync(List<EquipamientoTomado>? tomado)
    {
        foreach (var prestado in tomado ?? [])
        {
            var item = await _stockRepository.ObtenerAsync(prestado.ItemId);
            if (item == null)
                continue; // el ítem se dio de baja mientras estaba prestado
            item.FijarEnUso(Math.Max(item.EnUso - prestado.Cantidad, 0));
            await _stockRepository.ActualizarAsync(item);
        }
    }

    private async Task<Recibo> EmitirReciboAsync(Reserva reserva, Pago pago, string concepto, string cajero, List<ItemRecibo> items, decimal total)
    {
        var (nombre, dni) = Titular(reserva);
        var recibo = new Recibo
        {
            Id = await GeneradorIds.NuevoIdReciboAsync(_reciboRepository),
            ReservaId = reserva.Id,
            NombreCliente = nombre,
            DniCliente = dni,
            Concepto = concepto,
            Items = items,
            TotalPagado = total,
            Estado = "completado",
            Fecha = Formato.MarcaHoraria(),
            Cajero = cajero
        };
        recibo.AplicarPago(pago);
        await _reciboRepository.AgregarAsync(recibo);
        return recibo;
    }

    // Nombre y DNI del titular, para los recibos.
    private static (string Nombre, string Dni) Titular(Reserva reserva)
    {
        var titular = reserva.Participantes?.FirstOrDefault();
        return (Formato.SiVacio(titular?.Nombre, "Socio Tenis Ahora"), Formato.SiVacio(titular?.Dni, "00.000.000"));
    }

    private static string DescripcionAlquiler(Reserva reserva) =>
        $"Alquiler {Formato.Numero(reserva.DuracionHoras)} hs - {reserva.NombreCancha} - {reserva.Fecha} de {reserva.HoraInicio} a {reserva.HoraFin}";

    private static double HorasHastaElInicio(Reserva reserva)
    {
        var texto = $"{reserva.Fecha} {Formato.SiVacio(reserva.HoraInicio, "00:00")}";
        if (!DateTime.TryParseExact(texto, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var inicio))
            throw new DatosInvalidosException("La reserva no tiene una fecha y hora de inicio válidas.");
        return Math.Round((inicio - DateTime.Now).TotalHours * 10, MidpointRounding.AwayFromZero) / 10;
    }
}
