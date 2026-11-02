using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class Reembolso : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("clientName")]
    public string NombreCliente { get; set; } = string.Empty;

    [JsonPropertyName("clientDni")]
    public string DniCliente { get; set; } = string.Empty;

    [JsonPropertyName("relatedReservationId")]
    public string? ReservaRelacionada { get; set; }

    [JsonPropertyName("relatedReceiptId")]
    public string? ReciboRelacionado { get; set; }

    [JsonPropertyName("reason")]
    public string Motivo { get; set; } = string.Empty;

    // total o parcial.
    [JsonPropertyName("refundType")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("originalAmount")]
    public decimal MontoOriginal { get; set; }

    [JsonPropertyName("refundAmount")]
    public decimal MontoReembolsado { get; set; }

    [JsonPropertyName("refundMethod")]
    public string MedioReembolso { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("processedBy")]
    public string ProcesadoPor { get; set; } = string.Empty;
}
