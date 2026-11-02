using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class ClaseParticular : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Clasificacion { get; set; } = string.Empty;

    [JsonPropertyName("coachId")]
    public string? ProfesorId { get; set; }

    [JsonPropertyName("coachName")]
    public string? NombreProfesor { get; set; }

    [JsonPropertyName("studentName")]
    public string? NombreAlumno { get; set; }

    [JsonPropertyName("studentDni")]
    public string? DniAlumno { get; set; }

    [JsonPropertyName("studentPhone")]
    public string? TelefonoAlumno { get; set; }

    [JsonPropertyName("date")]
    public string? Fecha { get; set; }

    [JsonPropertyName("time")]
    public string? Horario { get; set; }

    [JsonPropertyName("courtAssigned")]
    public string? CanchaAsignada { get; set; }

    [JsonPropertyName("hourlyRate")]
    public decimal TarifaPorHora { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notas { get; set; }
}
