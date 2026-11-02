using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Recibo : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("reservationId")]
    public string? ReservaId { get; set; }

    // Inscripción, cuota o turno al que corresponde un recibo emitido desde el portal.
    [JsonPropertyName("relatedId")]
    public string? RelacionadoId { get; set; }

    [JsonPropertyName("kind")]
    public string? Tipo { get; set; }

    [JsonPropertyName("clientName")]
    public string NombreCliente { get; set; } = string.Empty;

    [JsonPropertyName("clientDni")]
    public string DniCliente { get; set; } = string.Empty;

    [JsonPropertyName("concept")]
    public string Concepto { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<ItemRecibo> Items { get; set; } = [];

    [JsonPropertyName("totalPaid")]
    public decimal TotalPagado { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? MedioPago { get; set; }

    [JsonPropertyName("transactionId")]
    public string? TransaccionId { get; set; }

    [JsonPropertyName("cardBrand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarcaTarjeta { get; set; }

    [JsonPropertyName("cardLast4")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UltimosDigitos { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("cashierName")]
    public string Cajero { get; set; } = string.Empty;

    // El medio de pago sale de la transacción aprobada, no de lo que declare quien registra el recibo.
    public void AplicarPago(Pago pago)
    {
        MedioPago = pago.MedioPago;
        TransaccionId = pago.Id;
        if (!string.IsNullOrEmpty(pago.UltimosDigitos))
        {
            MarcaTarjeta = pago.MarcaTarjeta;
            UltimosDigitos = pago.UltimosDigitos;
        }
    }
}
