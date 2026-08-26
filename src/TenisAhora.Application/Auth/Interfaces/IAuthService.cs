using TenisAhora.Application.Auth.Dtos;

namespace TenisAhora.Application.Auth.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegistrarAsync(RegistrarUsuarioDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}