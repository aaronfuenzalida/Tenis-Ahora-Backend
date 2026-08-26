using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Auth.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorEmailAsync(string email);
    Task AgregarAsync(Usuario usuario);
}