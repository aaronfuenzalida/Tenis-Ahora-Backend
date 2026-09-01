namespace TenisAhora.Application.Auth.Dtos;

public record AuthResponseDto(
    string Token,
    int Id,
    string Nombre,
    string Apellido,
    string Email,
    string NumeroTelefono,
    string? Direccion,
    string Rol);
