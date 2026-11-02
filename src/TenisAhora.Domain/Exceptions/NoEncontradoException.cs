namespace TenisAhora.Domain.Exceptions;

// El registro pedido no existe (404).
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}
