using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Competencias.Dtos;
using TenisAhora.Application.Competencias.Interfaces;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Competencias.Services;

public abstract class ServicioCompetencia<T> : ServicioCrud<T>, IServicioCompetencia where T : Competencia
{
    private readonly IDocumentoService _documentoService;

    protected ServicioCompetencia(IRepositorio<T> repositorio, IDocumentoService documentoService) : base(repositorio)
    {
        _documentoService = documentoService;
    }

    // "este torneo" o "esta liga", para los mensajes.
    protected abstract string Descripcion { get; }
    protected abstract string PrefijoParticipante { get; }
    protected abstract string PrefijoEnfrentamiento { get; }
    protected abstract string RondaPorDefecto { get; }
    protected abstract string ReglamentoPorDefecto { get; }

    protected override Task PrepararAltaAsync(T competencia, JsonObject cuerpo, UsuarioActual usuario)
    {
        competencia.Estado = Formato.SiVacio(competencia.Estado, "inscripcion_abierta");
        competencia.ReglamentoInterno = Formato.SiVacio(competencia.ReglamentoInterno, ReglamentoPorDefecto);
        competencia.Participantes ??= [];
        return Task.CompletedTask;
    }

    public async Task<object> ActualizarReglamentoAsync(string id, JsonObject cuerpo)
    {
        var competencia = await ObtenerExistenteAsync(id);
        if (cuerpo.ContainsKey("internalRules"))
            competencia.ReglamentoInterno = Json.Texto(cuerpo, "internalRules");
        // RF041 / RF048: documento del reglamento; null lo quita.
        if (cuerpo.TryGetPropertyValue("internalRulesDocument", out var referencia))
        {
            competencia.DocumentoReglamento = referencia is null
                ? null
                : await _documentoService.ResolverReferenciaAsync(Json.Texto(referencia, "id"));
        }
        await _repositorio.ActualizarAsync(competencia);
        return competencia;
    }

    // Un socio solo se puede inscribir a sí mismo.
    public async Task<InscripcionRealizadaDto> InscribirAsync(string id, InscripcionDto dto, UsuarioActual usuario)
    {
        if (!usuario.EsPersonal)
        {
            if (!usuario.EsSocio)
                throw AccesoDenegadoException.SoloPersonal();
            if (!usuario.EsSuDni(dto.Dni))
                throw new AccesoDenegadoException("Solo podés inscribirte a vos mismo.");
        }

        var competencia = await ObtenerExistenteAsync(id);
        var participantes = competencia.Participantes ?? [];
        if (InscriptoPorDni(participantes, dto.Dni) || InscriptoPorDni(participantes, dto.PartnerDni))
            throw new ConflictoException($"Ya hay una inscripción a {Descripcion} con ese DNI.");
        if (competencia.CupoMaximo > 0 && participantes.Count >= competencia.CupoMaximo)
            throw new ConflictoException($"{char.ToUpperInvariant(Descripcion[0])}{Descripcion[1..]} alcanzó el cupo máximo de inscriptos.");

        var participante = new ParticipanteCompetencia
        {
            Id = $"{PrefijoParticipante}-{Formato.Milisegundos()}",
            Nombre = Formato.SiVacio(Formato.SiVacio(dto.Name, dto.PartnerName ?? string.Empty), "Participante Inscripto"),
            Dni = Formato.SiVacio(dto.Dni, "00.000.000"),
            Telefono = dto.Phone ?? string.Empty,
            NombrePareja = dto.PartnerName ?? string.Empty,
            DniPareja = dto.PartnerDni ?? string.Empty,
            InscriptoEl = Formato.Hoy()
        };
        participantes.Add(participante);
        competencia.Participantes = participantes;
        competencia.Inscriptos = participantes.Count;
        AlInscribir(competencia, participante);

        await _repositorio.ActualizarAsync(competencia);
        return new InscripcionRealizadaDto(true, participante);
    }

    public async Task DesinscribirAsync(string id, string participanteId)
    {
        var competencia = await ObtenerExistenteAsync(id);
        var participantes = competencia.Participantes ?? [];
        var quitado = participantes.FirstOrDefault(p => p.Id == participanteId);
        competencia.Participantes = participantes.Where(p => p.Id != participanteId).ToList();
        competencia.Inscriptos = competencia.Participantes.Count;
        AlDesinscribir(competencia, quitado);
        await _repositorio.ActualizarAsync(competencia);
    }

    public async Task<EnfrentamientoGuardadoDto> AgregarEnfrentamientoAsync(string id, NuevoEnfrentamientoDto dto)
    {
        var jugador1 = (dto.P1 ?? string.Empty).Trim();
        var jugador2 = (dto.P2 ?? string.Empty).Trim();
        if (jugador1 == string.Empty || jugador2 == string.Empty)
            throw new DatosInvalidosException("p1 y p2 son obligatorios.");
        if (jugador1 == jugador2)
            throw new DatosInvalidosException("Los dos jugadores deben ser distintos.");

        var competencia = await ObtenerExistenteAsync(id);
        var enfrentamiento = new Enfrentamiento
        {
            Id = $"{PrefijoEnfrentamiento}-{Formato.Milisegundos()}",
            Jugador1 = jugador1,
            Jugador2 = jugador2,
            Resultado = "Pendiente",
            Ganador = null,
            Estado = "programado",
            Cancha = Formato.SiVacio(dto.Court, "Cancha 1"),
            Fecha = Formato.SiVacio(dto.Date, Formato.Hoy())
        };

        var rondas = competencia.ObtenerRondas();
        var nombreRonda = Formato.SiVacio(dto.Round, $"{RondaPorDefecto} {rondas.Count + 1}");
        var ronda = rondas.FirstOrDefault(r => r.Nombre == nombreRonda);
        if (ronda == null)
            rondas.Add(new Ronda { Nombre = nombreRonda, Enfrentamientos = [enfrentamiento] });
        else
            ronda.Enfrentamientos.Add(enfrentamiento);

        await _repositorio.ActualizarAsync(competencia);
        return new EnfrentamientoGuardadoDto(true, enfrentamiento);
    }

    public async Task<EnfrentamientoGuardadoDto> ActualizarEnfrentamientoAsync(string id, string enfrentamientoId, JsonObject cambios)
    {
        var competencia = await ObtenerExistenteAsync(id);
        var enfrentamiento = ActualizarEnfrentamiento(competencia.ObtenerRondas(), enfrentamientoId, cambios);
        await _repositorio.ActualizarAsync(competencia);
        return new EnfrentamientoGuardadoDto(true, enfrentamiento);
    }

    protected virtual void AlInscribir(T competencia, ParticipanteCompetencia participante) { }

    protected virtual void AlDesinscribir(T competencia, ParticipanteCompetencia? quitado) { }

    // Mezcla los cambios sobre el enfrentamiento (el id no cambia) y lo devuelve.
    protected static Enfrentamiento ActualizarEnfrentamiento(List<Ronda> rondas, string enfrentamientoId, JsonObject cambios)
    {
        Enfrentamiento? actualizado = null;
        foreach (var ronda in rondas)
        {
            for (var i = 0; i < ronda.Enfrentamientos.Count; i++)
            {
                if (ronda.Enfrentamientos[i].Id != enfrentamientoId)
                    continue;
                var nuevo = Json.Mezclar(ronda.Enfrentamientos[i], cambios);
                nuevo.Id = enfrentamientoId;
                ronda.Enfrentamientos[i] = nuevo;
                actualizado = nuevo;
            }
        }
        return actualizado ?? throw new NoEncontradoException($"No existe el enfrentamiento {enfrentamientoId}.");
    }

    // El DNI ya figura entre los inscriptos, como titular o como pareja.
    private static bool InscriptoPorDni(List<ParticipanteCompetencia> participantes, string? dni)
    {
        var buscado = Formato.SoloDigitos(dni);
        return buscado != string.Empty && participantes.Any(p =>
            Formato.SoloDigitos(p.Dni) == buscado || Formato.SoloDigitos(p.DniPareja) == buscado);
    }
}
