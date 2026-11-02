namespace TenisAhora.Domain.Exceptions;

// La pasarela simulada rechazó el pago (402).
public class PagoRechazadoException : Exception
{
    public PagoRechazadoException(string mensaje) : base(mensaje) { }
}
