using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerarToken(Usuario usuario);
}