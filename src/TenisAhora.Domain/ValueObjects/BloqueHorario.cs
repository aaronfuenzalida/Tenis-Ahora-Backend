using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class BloqueHorario
{
    [JsonPropertyName("day")]
    public string Dia { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string Desde { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public string Hasta { get; set; } = string.Empty;

    [JsonPropertyName("reason")]
    public string Motivo { get; set; } = string.Empty;
}
