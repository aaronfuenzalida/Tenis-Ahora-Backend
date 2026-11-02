using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

// Lo común a torneos y ligas: datos, reglamento e inscriptos. Cada una tiene su propia tabla.
public abstract class Competencia : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("competitionTypeId")]
    public string TipoCompetenciaId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("modality")]
    public string Modalidad { get; set; } = string.Empty;

    [JsonPropertyName("surfaceRequired")]
    public string SuperficieRequerida { get; set; } = string.Empty;

    [JsonPropertyName("startDate")]
    public string FechaInicio { get; set; } = string.Empty;

    [JsonPropertyName("endDate")]
    public string FechaFin { get; set; } = string.Empty;

    [JsonPropertyName("registrationFee")]
    public decimal Arancel { get; set; }

    [JsonPropertyName("maxParticipants")]
    public int CupoMaximo { get; set; }

    [JsonPropertyName("currentEnrolled")]
    public int Inscriptos { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("internalRules")]
    public string ReglamentoInterno { get; set; } = string.Empty;

    // Sin documento la clave no viaja: el front distingue "sin documento" por su ausencia.
    [JsonPropertyName("internalRulesDocument")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentoRef? DocumentoReglamento { get; set; }

    [JsonPropertyName("participants")]
    public List<ParticipanteCompetencia> Participantes { get; set; } = [];

    // El cuadro de un torneo o el fixture de una liga.
    public abstract List<Ronda> ObtenerRondas();
}
