using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class Ronda
{
    [JsonPropertyName("round")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("matches")]
    public List<Enfrentamiento> Enfrentamientos { get; set; } = [];
}
