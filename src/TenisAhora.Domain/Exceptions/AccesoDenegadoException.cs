namespace TenisAhora.Domain.Exceptions;

// El usuario no tiene permiso para la acción (403).
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje) : base(mensaje) { }

    public static AccesoDenegadoException SoloPersonal() => new("Esta acción es solo para el personal del club.");
}
