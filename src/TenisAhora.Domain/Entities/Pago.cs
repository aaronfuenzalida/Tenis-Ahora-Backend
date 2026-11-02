using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

// Transacción de la pasarela simulada. Se usa una sola vez: la seña, el saldo o el recibo que la
// consume queda anotado en UsadoEn.
public class Pago : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("paymentMethod")]
    public string MedioPago { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Monto { get; set; }

    [JsonPropertyName("concept")]
    public string Concepto { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public string CreadoEn { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreadoPor { get; set; } = string.Empty;

    [JsonPropertyName("usedBy")]
    public string? UsadoEn { get; set; }

    [JsonPropertyName("usedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FechaUso { get; set; }

    // De la tarjeta se guardan solo la marca y los últimos 4 dígitos (RNF03).
    [JsonPropertyName("cardBrand")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MarcaTarjeta { get; set; }

    [JsonPropertyName("cardLast4")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UltimosDigitos { get; set; }
}
