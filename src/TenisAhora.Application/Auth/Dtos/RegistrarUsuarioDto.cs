namespace TenisAhora.Application.Auth.Dtos;

public record RegistrarUsuarioDto(string Nombre,
 string Apellido,
 string? Direccion,
 string Email,
 string NumeroTelefono,
 string Password);
