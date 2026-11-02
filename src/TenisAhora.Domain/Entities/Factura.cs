using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Factura : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("clientName")]
    public string NombreCliente { get; set; } = string.Empty;

    [JsonPropertyName("clientDni")]
    public string DniCliente { get; set; } = string.Empty;

    [JsonPropertyName("clientAddress")]
    public string? DireccionCliente { get; set; }

    [JsonPropertyName("concept")]
    public string Concepto { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<ItemFactura> Items { get; set; } = [];

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string MedioPago { get; set; } = string.Empty;

    [JsonPropertyName("relatedReceiptId")]
    public string? ReciboRelacionado { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = string.Empty;

    [JsonPropertyName("issuedBy")]
    public string EmitidaPor { get; set; } = string.Empty;
}
