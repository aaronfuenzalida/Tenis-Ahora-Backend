using Microsoft.EntityFrameworkCore;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

public class UsuarioRepository : Repositorio<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(TenisAhoraDbContext context) : base(context) { }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email) =>
        await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
}
