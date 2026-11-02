namespace TenisAhora.Domain.Exceptions;

// Error de validación de una regla de negocio o de un dato de entrada (400).
public class DatosInvalidosException : Exception
{
    public DatosInvalidosException(string mensaje) : base(mensaje) { }
}
