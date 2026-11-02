using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Auth.Interfaces;

public interface IUsuarioRepository : IRepositorio<Usuario>
{
    Task<Usuario?> ObtenerPorEmailAsync(string email);
}
