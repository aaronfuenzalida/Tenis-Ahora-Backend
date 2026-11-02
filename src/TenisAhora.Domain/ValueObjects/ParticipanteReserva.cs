using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class ParticipanteReserva
{
    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("dni")]
    public string Dni { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string? Telefono { get; set; }

    [JsonPropertyName("isLead")]
    public bool? EsTitular { get; set; }
}
