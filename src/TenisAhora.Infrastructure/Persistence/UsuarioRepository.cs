using Microsoft.EntityFrameworkCore;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly TenisAhoraDbContext _context;

    public UsuarioRepository(TenisAhoraDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObtenerPorEmailAsync(string email) =>
        await _context.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);

    public async Task AgregarAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
    }
}
