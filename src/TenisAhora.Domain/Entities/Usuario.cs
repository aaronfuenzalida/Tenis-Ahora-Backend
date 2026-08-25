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

    public required string PassWordHash { get; set; }

    public required Rol Rol { get; set; }
}