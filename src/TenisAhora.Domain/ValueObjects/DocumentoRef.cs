using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// Lo que guardan torneos, ligas y fichas de profesores de un documento: sin el contenido.
public class DocumentoRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string TipoMime { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Tamanio { get; set; }
}
