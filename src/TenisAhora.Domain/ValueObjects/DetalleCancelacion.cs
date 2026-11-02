using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class DetalleCancelacion
{
    [JsonPropertyName("cancelledAt")]
    public string CanceladaEn { get; set; } = string.Empty;

    [JsonPropertyName("hoursInAdvance")]
    public double HorasDeAnticipacion { get; set; }

    [JsonPropertyName("policyApplied")]
    public string PoliticaAplicada { get; set; } = string.Empty;
}
