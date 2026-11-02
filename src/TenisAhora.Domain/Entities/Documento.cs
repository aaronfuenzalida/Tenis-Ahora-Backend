using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

// Reglamento interno o título habilitante (PDF, JPG o PNG). El archivo se guarda en la misma base:
// es lo único que hay que respaldar.
public class Documento : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("mimeType")]
    public string TipoMime { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Tamanio { get; set; }

    // En el seed viene en base64.
    [JsonPropertyName("data")]
    public byte[] Contenido { get; set; } = [];

    [JsonPropertyName("uploadedAt")]
    public string SubidoEn { get; set; } = string.Empty;

    public DocumentoRef Referencia() => new()
    {
        Id = Id,
        Nombre = Nombre,
        TipoMime = TipoMime,
        Tamanio = Tamanio
    };
}
