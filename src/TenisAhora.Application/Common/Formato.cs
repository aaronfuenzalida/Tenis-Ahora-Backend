using System.Globalization;

namespace TenisAhora.Application.Common;

// Fechas, números y textos con el mismo formato que usaba la API vieja (y que espera el front).
public static class Formato
{
    public static long Milisegundos() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public static string Hoy() => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // 2026-10-23 16:46: la forma en que los comprobantes muestran la fecha.
    public static string MarcaHoraria() => DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public static string Iso() => DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    // Para comparar DNI cargados con o sin puntos.
    public static string SoloDigitos(string? valor) => new string((valor ?? string.Empty).Where(char.IsAsciiDigit).ToArray());

    public static string SiVacio(string? valor, string defecto) => string.IsNullOrEmpty(valor) ? defecto : valor;

    // 1 para 1.0 y 1.5 para 1.5, con punto decimal.
    public static string Numero(decimal valor) => valor.ToString("0.##########", CultureInfo.InvariantCulture);

    public static string Numero(double valor) => valor.ToString("0.##########", CultureInfo.InvariantCulture);
}
