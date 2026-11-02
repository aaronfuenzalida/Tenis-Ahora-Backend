using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class Enfrentamiento
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("p1")]
    public string Jugador1 { get; set; } = string.Empty;

    [JsonPropertyName("p2")]
    public string Jugador2 { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public string Resultado { get; set; } = string.Empty;

    [JsonPropertyName("winner")]
    public string? Ganador { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("court")]
    public string Cancha { get; set; } = string.Empty;

    [JsonPropertyName("date")]
    public string Fecha { get; set; } = string.Empty;
}
