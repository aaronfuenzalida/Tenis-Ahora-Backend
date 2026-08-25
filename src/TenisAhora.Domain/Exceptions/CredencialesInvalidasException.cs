namespace TenisAhora.Domain.Exceptions;

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("Credenciales inválidas.") { }
}