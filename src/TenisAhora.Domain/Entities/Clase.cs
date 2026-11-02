using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

// Clase grupal o entrenamiento (campo kind), con sus alumnos y la asistencia por sesión.
public class Clase : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Clasificacion { get; set; } = string.Empty;

    [JsonPropertyName("coachId")]
    public string? ProfesorId { get; set; }

    [JsonPropertyName("coachName")]
    public string? NombreProfesor { get; set; }

    [JsonPropertyName("scheduleDays")]
    public string? Dias { get; set; }

    [JsonPropertyName("scheduleTime")]
    public string? Horario { get; set; }

    // Trae la superficie entre paréntesis, por ejemplo "Cancha 3 (Cemento)".
    [JsonPropertyName("courtAssigned")]
    public string? CanchaAsignada { get; set; }

    [JsonPropertyName("maxCapacity")]
    public int CupoMaximo { get; set; }

    [JsonPropertyName("currentEnrolled")]
    public int Inscriptos { get; set; }

    [JsonPropertyName("monthlyFee")]
    public decimal CuotaMensual { get; set; }

    [JsonPropertyName("monthName")]
    public string? Mes { get; set; }

    [JsonPropertyName("sessions")]
    public List<string> Sesiones { get; set; } = ["02/10", "04/10", "09/10", "11/10", "16/10", "18/10", "23/10", "25/10"];

    [JsonPropertyName("students")]
    public List<Alumno> Alumnos { get; set; } = [];
}
