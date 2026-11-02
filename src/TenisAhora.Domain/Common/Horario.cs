namespace TenisAhora.Domain.Common;

// Horarios "HH:MM" tal como los manda el front.
public static class Horario
{
    public static int AMinutos(string? hora)
    {
        var partes = (hora ?? string.Empty).Trim().Split(':', 2);
        int.TryParse(partes[0], out var horas);
        var minutos = 0;
        if (partes.Length > 1)
            int.TryParse(partes[1], out minutos);
        return horas * 60 + minutos;
    }

    public static string Sumar(string inicio, decimal horas)
    {
        var total = AMinutos(inicio) + (int)Math.Round(horas * 60, MidpointRounding.AwayFromZero);
        return $"{total / 60:00}:{total % 60:00}";
    }
}
