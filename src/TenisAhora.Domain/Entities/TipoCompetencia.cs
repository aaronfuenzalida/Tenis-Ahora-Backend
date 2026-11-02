using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class TipoCompetencia : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Formato { get; set; } = string.Empty;

    [JsonPropertyName("appliesTo")]
    public string AplicaA { get; set; } = string.Empty;

    [JsonPropertyName("scoringSystem")]
    public string SistemaPuntuacion { get; set; } = string.Empty;

    [JsonPropertyName("minParticipants")]
    public int MinimoParticipantes { get; set; }

    [JsonPropertyName("maxParticipants")]
    public int MaximoParticipantes { get; set; }

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Activo { get; set; } = true;
}
