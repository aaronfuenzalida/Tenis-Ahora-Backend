namespace TenisAhora.Domain.Exceptions;

public class EmailYaRegistradoException : Exception
{
    public EmailYaRegistradoException(string email) : base($"El email {email} ya está registrado.") { }
}