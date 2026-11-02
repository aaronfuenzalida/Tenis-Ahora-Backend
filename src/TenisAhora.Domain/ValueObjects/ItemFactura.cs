using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class ItemFactura
{
    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Cantidad { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal PrecioUnitario { get; set; }
}
