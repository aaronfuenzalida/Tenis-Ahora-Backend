using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// Días y horario de atención de una cancha, con sus bloqueos programados (RD06).
public class Disponibilidad
{
    [JsonPropertyName("days")]
    public List<string> Dias { get; set; } = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];

    [JsonPropertyName("openTime")]
    public string Apertura { get; set; } = "08:00";

    [JsonPropertyName("closeTime")]
    public string Cierre { get; set; } = "22:00";

    [JsonPropertyName("slotMinutes")]
    public int MinutosPorTurno { get; set; } = 60;

    [JsonPropertyName("blockedRanges")]
    public List<BloqueHorario> Bloqueos { get; set; } = [];

    [JsonPropertyName("notes")]
    public string Notas { get; set; } = string.Empty;
}
