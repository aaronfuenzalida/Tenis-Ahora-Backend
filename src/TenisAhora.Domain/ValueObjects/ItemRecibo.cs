using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class ItemRecibo
{
    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Monto { get; set; }
}
