using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class ParticipanteCompetencia
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("dni")]
    public string Dni { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Telefono { get; set; }

    [JsonPropertyName("partnerName")]
    public string? NombrePareja { get; set; }

    [JsonPropertyName("partnerDni")]
    public string? DniPareja { get; set; }

    [JsonPropertyName("enrolledAt")]
    public string InscriptoEl { get; set; } = string.Empty;
}
