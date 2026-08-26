using TenisAhora.Domain.Enums;

namespace TenisAhora.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }

    public required string Nombre { get; set; }

    public required string Apellido { get; set; }

    public string? Direccion { get; set; }

    public required string NumeroTelefono { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required Rol Rol { get; set; }

    public bool EstaActivo => FechaBaja is null;

    public DateTime? FechaBaja { get; private set; }

    public void DarDeBaja()
    {
        if (FechaBaja is not null)
            throw new InvalidOperationException("El usuario ya está dado de baja.");

        FechaBaja = DateTime.UtcNow;
    }
}