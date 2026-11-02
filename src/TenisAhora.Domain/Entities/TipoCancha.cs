using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class TipoCancha : IEntidad
{
    // RD07: ninguna comisión supera los 30 alumnos.
    public const int CupoMaximo = 30;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Etiqueta { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("speed")]
    public string Velocidad { get; set; } = string.Empty;

    [JsonPropertyName("basePricePerHour")]
    public decimal PrecioBasePorHora { get; set; }

    [JsonPropertyName("maintenanceFrequency")]
    public string FrecuenciaMantenimiento { get; set; } = string.Empty;

    [JsonPropertyName("maxStudentsPerClass")]
    public int MaximoAlumnosPorClase { get; set; }

    [JsonPropertyName("active")]
    public bool Activo { get; set; } = true;

    // Sin cupo informado se usa el máximo permitido.
    public static int LimitarCupo(int cupo) => cupo <= 0 ? CupoMaximo : Math.Min(cupo, CupoMaximo);
}
