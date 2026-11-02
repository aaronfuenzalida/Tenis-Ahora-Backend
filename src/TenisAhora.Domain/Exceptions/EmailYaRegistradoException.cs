namespace TenisAhora.Domain.Exceptions;

public class EmailYaRegistradoException : Exception
{
    public EmailYaRegistradoException() : base("Ya existe un usuario registrado con ese email.") { }
}
