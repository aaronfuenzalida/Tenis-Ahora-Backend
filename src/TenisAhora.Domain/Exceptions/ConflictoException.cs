namespace TenisAhora.Domain.Exceptions;

// La acción choca con el estado actual: duplicado, sin cupo, sin stock, superposición (409).
public class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}
