using System.Globalization;
using System.Text.Json.Serialization;
using TenisAhora.Domain.Common;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Cancha : IEntidad
{
    // Índice = DayOfWeek.
    private static readonly string[] DiasSemana = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("surfaceType")]
    public string TipoSuperficie { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public int Capacidad { get; set; }

    [JsonPropertyName("pricePerHour")]
    public decimal PrecioPorHora { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    [JsonPropertyName("maintenanceNotes")]
    public string NotasMantenimiento { get; set; } = string.Empty;

    [JsonPropertyName("availability")]
    public Disponibilidad Disponibilidad { get; set; } = new();

    [JsonPropertyName("photo")]
    public string Foto { get; set; } = string.Empty;

    // Por qué la cancha no admite el turno según su disponibilidad (días de atención, horario y
    // bloqueos programados, RD06), o null si está libre. El estado "mantenimiento" se valida aparte.
    public string? MotivoDeBloqueo(string fecha, string horaInicio, decimal duracionHoras)
    {
        var disponibilidad = Disponibilidad ?? new Disponibilidad();
        var dia = string.Empty;
        if (DateTime.TryParseExact(fecha, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaTurno))
        {
            dia = DiasSemana[(int)fechaTurno.DayOfWeek];
            if (!(disponibilidad.Dias ?? []).Contains(dia))
                return $"sin atención el día {dia}";
        }
        if (string.IsNullOrEmpty(horaInicio))
            return null;
        if (duracionHoras <= 0)
            duracionHoras = 1;
        var desde = Horario.AMinutos(horaInicio);
        var hasta = desde + (int)Math.Round(duracionHoras * 60, MidpointRounding.AwayFromZero);
        if (desde < Horario.AMinutos(disponibilidad.Apertura) || hasta > Horario.AMinutos(disponibilidad.Cierre))
            return $"fuera de horario ({disponibilidad.Apertura} a {disponibilidad.Cierre})";
        foreach (var bloqueo in disponibilidad.Bloqueos ?? [])
        {
            if (bloqueo.Dia == dia && desde < Horario.AMinutos(bloqueo.Hasta) && hasta > Horario.AMinutos(bloqueo.Desde))
                return string.IsNullOrEmpty(bloqueo.Motivo) ? $"bloqueada de {bloqueo.Desde} a {bloqueo.Hasta}" : bloqueo.Motivo;
        }
        return null;
    }
}
