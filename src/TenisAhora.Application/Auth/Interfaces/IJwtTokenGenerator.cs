using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Auth.Interfaces;

public interface IJwtTokenGenerator
{
    // ExpiraEn: segundos Unix.
    (string Token, long ExpiraEn) GenerarToken(Usuario usuario, string rol);
}
