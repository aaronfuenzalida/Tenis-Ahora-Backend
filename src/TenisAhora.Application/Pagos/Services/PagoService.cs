using System.Security.Cryptography;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Pagos.Dtos;
using TenisAhora.Application.Pagos.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Pagos.Services;

// Pasarela de pagos simulada (RD11: tarjeta de débito, tarjeta de crédito y QR de Mercado Pago). No
// mueve dinero: solo acepta las tarjetas de prueba que publica Mercado Pago y resuelve el resultado
// con el nombre del titular, como su entorno de pruebas.
public class PagoService : IPagoService
{
    public const string MedioQr = "Mercado Pago (QR)";
    public const string MedioDebito = "Tarjeta de Débito";
    public const string MedioCredito = "Tarjeta de Crédito";

    private record TarjetaDePrueba(string Numero, string Marca, string Medio, string Codigo);

    // Tarjetas de prueba de Mercado Pago Argentina (todas vencen 11/30).
    private static readonly TarjetaDePrueba[] TarjetasDePrueba =
    [
        new("5031755734530604", "Mastercard", MedioCredito, "123"),
        new("4509953566233704", "Visa", MedioCredito, "123"),
        new("371180303257522", "American Express", MedioCredito, "1234"),
        new("5287338310253304", "Mastercard", MedioDebito, "123"),
        new("4002768694395619", "Visa", MedioDebito, "123"),
    ];

    // APRO o cualquier otro nombre aprueba; estos rechazan, y CONT queda pendiente sin confirmar la operación.
    private static readonly Dictionary<string, string> RechazosPorTitular = new()
    {
        ["OTHE"] = "Pago rechazado por error general.",
        ["CALL"] = "Pago rechazado: el titular tiene que autorizarlo con el emisor de la tarjeta.",
        ["FUND"] = "Pago rechazado por saldo insuficiente.",
        ["SECU"] = "Pago rechazado: el código de seguridad es inválido.",
        ["EXPI"] = "Pago rechazado por un problema con la fecha de vencimiento.",
        ["FORM"] = "Pago rechazado: los datos de la tarjeta son inválidos.",
        ["CONT"] = "El pago quedó pendiente de acreditación y la operación no se confirma (RD01).",
    };

    private readonly IRepositorio<Pago> _pagoRepository;

    public PagoService(IRepositorio<Pago> pagoRepository)
    {
        _pagoRepository = pagoRepository;
    }

    public async Task<Pago> CrearAsync(CrearPagoDto dto, UsuarioActual usuario)
    {
        usuario.ExigirSocioOPersonal();
        var monto = Math.Round((dto.Amount ?? 0) * 100, MidpointRounding.AwayFromZero) / 100;
        if (monto <= 0)
            throw new DatosInvalidosException("El importe a pagar debe ser mayor a cero.");

        var medio = dto.PaymentMethod ?? string.Empty;
        var pago = new Pago
        {
            Estado = "approved",
            MedioPago = medio,
            Monto = monto,
            Concepto = (dto.Concept ?? string.Empty).Trim(),
            CreadoEn = Formato.Iso(),
            CreadoPor = usuario.Id
        };

        switch (medio)
        {
            case MedioQr:
                // La billetera confirma el pago al escanear el QR; acá se simula esa notificación aprobada.
                break;
            case MedioDebito:
            case MedioCredito:
                AutorizarTarjeta(medio, dto.Card ?? new TarjetaDto(null, null, null, null), pago);
                break;
            default:
                throw new DatosInvalidosException("El medio de pago debe ser Tarjeta de Débito, Tarjeta de Crédito o Mercado Pago (QR) (RD11).");
        }

        do
        {
            pago.Id = $"MP-{RandomNumberGenerator.GetInt32(100_000_000):D8}";
        } while (await _pagoRepository.ExisteAsync(pago.Id));

        await _pagoRepository.AgregarAsync(pago);
        return pago;
    }

    public async Task<Pago> ConsumirAsync(UsuarioActual usuario, string? transaccionId, decimal monto, string referencia)
    {
        if (string.IsNullOrEmpty(transaccionId))
            throw new DatosInvalidosException("Falta el pago: primero hay que abonar con tarjeta o billetera virtual (RD01).");

        var pago = await _pagoRepository.ObtenerAsync(transaccionId)
            ?? throw new DatosInvalidosException($"El pago {transaccionId} no existe.");
        if (pago.Estado != "approved")
            throw new DatosInvalidosException($"El pago {transaccionId} no está aprobado.");
        if (!usuario.EsPersonal && pago.CreadoPor != usuario.Id)
            throw new AccesoDenegadoException("El pago no es tuyo.");
        if (!string.IsNullOrEmpty(pago.UsadoEn))
            throw new ConflictoException($"El pago {transaccionId} ya se usó en {pago.UsadoEn}.");
        if (Math.Abs(pago.Monto - monto) >= 0.01m)
            throw new DatosInvalidosException($"El pago {transaccionId} es por ${Formato.Numero(pago.Monto)} y el importe a cobrar es ${Formato.Numero(monto)}.");

        pago.UsadoEn = referencia;
        pago.FechaUso = Formato.Iso();
        await _pagoRepository.ActualizarAsync(pago);
        return pago;
    }

    // Valida la tarjeta ficticia; del número se guardan solo la marca y los últimos 4 dígitos.
    private static void AutorizarTarjeta(string medio, TarjetaDto tarjeta, Pago pago)
    {
        var numero = Formato.SoloDigitos(tarjeta.Number);
        var prueba = TarjetasDePrueba.FirstOrDefault(t => t.Numero == numero)
            ?? throw new DatosInvalidosException("Solo se aceptan tarjetas de prueba (ficticias), por ejemplo Visa crédito 4509 9535 6623 3704 o Visa débito 4002 7686 9439 5619.");
        if (prueba.Medio != medio)
            throw new DatosInvalidosException($"La tarjeta ingresada es {prueba.Medio.Replace("Tarjeta ", string.Empty).ToLowerInvariant()}: elegí {prueba.Medio} como medio de pago.");

        var titular = (tarjeta.Holder ?? string.Empty).Trim().ToUpperInvariant();
        if (titular == string.Empty)
            throw new DatosInvalidosException("Falta el nombre del titular de la tarjeta.");
        if (EstaVencida(tarjeta.Expiry))
            throw new PagoRechazadoException("Pago rechazado: la tarjeta está vencida.");
        if (Formato.SoloDigitos(tarjeta.Cvv) != prueba.Codigo)
            throw new PagoRechazadoException(RechazosPorTitular["SECU"]);
        if (RechazosPorTitular.TryGetValue(titular, out var rechazo))
            throw new PagoRechazadoException(rechazo);

        pago.MarcaTarjeta = prueba.Marca;
        pago.UltimosDigitos = numero[^4..];
    }

    // "MM/AA": la tarjeta vale hasta el último día del mes.
    private static bool EstaVencida(string? vencimiento)
    {
        var partes = (vencimiento ?? string.Empty).Trim().Split('/', 2);
        if (partes.Length != 2
            || !int.TryParse(partes[0].Trim(), out var mes)
            || !int.TryParse(partes[1].Trim(), out var anio)
            || mes < 1 || mes > 12)
        {
            throw new DatosInvalidosException("El vencimiento de la tarjeta debe tener el formato MM/AA.");
        }
        if (anio < 100)
            anio += 2000;
        return DateTime.Now >= new DateTime(anio, mes, 1).AddMonths(1);
    }
}
