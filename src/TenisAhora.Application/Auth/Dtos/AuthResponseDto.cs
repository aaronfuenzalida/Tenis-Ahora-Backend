namespace TenisAhora.Application.Auth.Dtos;

public record AuthResponseDto(
    string Token,
    string Id,
    string Nombre,
    string Apellido,
    string Email,
    string NumeroTelefono,
    string Direccion,
    string Rol,
    string Dni,
    string CoachId,
    string MemberNumber,
    string Photo,
    long ExpiraEn);
