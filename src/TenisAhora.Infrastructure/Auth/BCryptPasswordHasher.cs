using TenisAhora.Application.Auth.Interfaces;

namespace TenisAhora.Infrastructure.Auth;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);
    public bool Verificar(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}