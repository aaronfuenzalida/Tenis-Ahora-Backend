using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Reserva : IEntidad
{
    // Pendiente de pago (registrada, sin seña) -> confirmada (seña del 50 % abonada, RD01) -> en juego
    // (alquiler iniciado, equipamiento descontado del stock) -> finalizada (saldo abonado, equipamiento
    // devuelto). Se cancela mientras está pendiente o confirmada.
    public const string EstadoPendientePago = "pendiente_pago";
    public const string EstadoConfirmada = "confirmada";
    public const string EstadoEnJuego = "en_juego";
    public const string EstadoFinalizada = "finalizada";
    public const string EstadoCancelada = "cancelada";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("courtId")]
    public string CanchaId { get; set; } = string.Empty;

    [JsonPropertyName("courtName")]
    public string NombreCancha { get; set; } = string.Empty;

    [JsonPropertyName("surfaceType")]
    public string TipoSuperficie { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("startTime")]
    public string HoraInicio { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string HoraFin { get; set; } = string.Empty;

    [JsonPropertyName("durationHours")]
    public decimal DuracionHoras { get; set; }

    // singles (2 jugadores) o dobles (4 jugadores).
    [JsonPropertyName("playersType")]
    public string Modalidad { get; set; } = string.Empty;

    // El primero es el titular: a su DNI van los recibos.
    [JsonPropertyName("participants")]
    public List<ParticipanteReserva> Participantes { get; set; } = [];

    [JsonPropertyName("notes")]
    public string? Notas { get; set; }

    [JsonPropertyName("equipmentAssigned")]
    public EquipamientoAsignado? Equipamiento { get; set; }

    [JsonPropertyName("equipmentTaken")]
    public List<EquipamientoTomado>? EquipamientoTomado { get; set; }

    [JsonPropertyName("courtCost")]
    public decimal CostoCancha { get; set; }

    [JsonPropertyName("equipmentCost")]
    public decimal CostoEquipamiento { get; set; }

    [JsonPropertyName("totalCost")]
    public decimal CostoTotal { get; set; }

    [JsonPropertyName("depositAmount")]
    public decimal MontoSena { get; set; }

    [JsonPropertyName("depositPaid")]
    public decimal SenaPagada { get; set; }

    [JsonPropertyName("depositPaymentMethod")]
    public string? MedioPagoSena { get; set; }

    [JsonPropertyName("depositTransactionId")]
    public string? TransaccionSena { get; set; }

    [JsonPropertyName("depositReceiptNumber")]
    public string? ReciboSena { get; set; }

    [JsonPropertyName("remainingBalance")]
    public decimal SaldoRestante { get; set; }

    [JsonPropertyName("remainingPaid")]
    public bool SaldoPagado { get; set; }

    [JsonPropertyName("finalReceiptNumber")]
    public string? ReciboFinal { get; set; }

    [JsonPropertyName("finalPaymentMethod")]
    public string? MedioPagoFinal { get; set; }

    [JsonPropertyName("finalTransactionId")]
    public string? TransaccionFinal { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("cancellationDetails")]
    public DetalleCancelacion? Cancelacion { get; set; }

    [JsonPropertyName("createdAt")]
    public string? CreadaEn { get; set; }

    [JsonPropertyName("confirmedAt")]
    public string? ConfirmadaEn { get; set; }

    [JsonPropertyName("startedAt")]
    public string? IniciadaEn { get; set; }

    [JsonPropertyName("finishedAt")]
    public string? FinalizadaEn { get; set; }

    [JsonPropertyName("updatedAt")]
    public string? ActualizadaEn { get; set; }

    // El costo sale de la tarifa de la cancha, no de lo que mande el cliente. La seña es el 50 %; una
    // reserva ya confirmada conserva la seña que pagó.
    public void CalcularCostos(decimal precioPorHora)
    {
        CostoCancha = precioPorHora * DuracionHoras;
        CostoEquipamiento = 0;
        CostoTotal = CostoCancha;
        var sena = SenaPagada;
        if (Estado == EstadoPendientePago)
        {
            sena = Math.Round(CostoCancha / 2, MidpointRounding.AwayFromZero);
            MontoSena = sena;
        }
        SaldoRestante = Math.Max(CostoCancha - sena, 0);
    }
}
