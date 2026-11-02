namespace TenisAhora.Application.Usuarios.Dtos;

// Solo lo necesario para registrar a un participante; teléfono, email y dirección quedan para el personal.
public record SocioDto(string Id, string Name, string? Dni, string MemberNumber);
