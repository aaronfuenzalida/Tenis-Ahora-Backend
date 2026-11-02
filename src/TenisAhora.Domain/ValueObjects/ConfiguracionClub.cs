using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// Plazos de reserva y cancelación (RD02-RD05) y tarifa de la clase particular: los fija el club y el
// sistema solo los consulta.
public class ConfiguracionClub
{
    public static readonly ConfiguracionClub Vigente = new();

    [JsonPropertyName("id")]
    public string Id => "club";

    [JsonPropertyName("freeCancellationHours")]
    public double HorasCancelacionSinCargo => 6;

    [JsonPropertyName("lateCancellationRetentionPercent")]
    public decimal PorcentajeRetencion => 50;

    [JsonPropertyName("maxAdvanceBookingDays")]
    public int DiasMaximosDeAnticipacion => 30;

    [JsonPropertyName("privateLessonHourlyRate")]
    public decimal TarifaClaseParticular => 6000;
}
