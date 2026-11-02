using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// De qué ítem del stock salió el equipamiento, para devolver exactamente lo mismo al finalizar.
public class EquipamientoTomado
{
    [JsonPropertyName("itemId")]
    public string ItemId { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Cantidad { get; set; }
}
