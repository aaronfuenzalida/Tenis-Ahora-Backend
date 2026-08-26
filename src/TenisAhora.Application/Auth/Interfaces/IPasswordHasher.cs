namespace TenisAhora.Application.Auth.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verificar(string password, string hash);
}