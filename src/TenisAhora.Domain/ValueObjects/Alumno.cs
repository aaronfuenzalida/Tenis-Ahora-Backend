using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class Alumno
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("dni")]
    public string Dni { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Telefono { get; set; }

    [JsonPropertyName("enrolledAt")]
    public string? InscriptoEl { get; set; }

    // Una marca por sesión: P (presente) o A (ausente).
    [JsonPropertyName("attendance")]
    public List<string> Asistencia { get; set; } = [];
}
