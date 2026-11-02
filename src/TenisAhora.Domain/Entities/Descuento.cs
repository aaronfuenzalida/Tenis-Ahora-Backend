using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class Descuento : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("discountPercent")]
    public decimal Porcentaje { get; set; }

    // Código en mayúsculas que se asigna a los usuarios (activeDiscounts).
    [JsonPropertyName("code")]
    public string Codigo { get; set; } = string.Empty;

    [JsonPropertyName("applicableTo")]
    public string AplicaA { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Activo { get; set; } = true;
}
