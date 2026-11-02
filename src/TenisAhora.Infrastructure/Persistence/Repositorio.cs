using Microsoft.EntityFrameworkCore;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

public class Repositorio<T> : IRepositorio<T> where T : class, IEntidad
{
    protected readonly TenisAhoraDbContext _context;

    public Repositorio(TenisAhoraDbContext context)
    {
        _context = context;
    }

    public async Task<List<T>> ListarAsync() =>
        await _context.Set<T>().ToListAsync();

    public async Task<T?> ObtenerAsync(string id) =>
        await _context.Set<T>().FindAsync(id);

    public async Task<bool> ExisteAsync(string id) =>
        await _context.Set<T>().FindAsync(id) is not null;

    public async Task<int> ContarAsync() =>
        await _context.Set<T>().CountAsync();

    public async Task AgregarAsync(T entidad)
    {
        _context.Set<T>().Add(entidad);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(T entidad)
    {
        _context.Set<T>().Update(entidad);
        await _context.SaveChangesAsync();
    }

    public async Task ReemplazarAsync(T actual, T nuevo)
    {
        _context.Entry(actual).CurrentValues.SetValues(nuevo);
        await _context.SaveChangesAsync();
    }

    public async Task EliminarAsync(T entidad)
    {
        _context.Set<T>().Remove(entidad);
        await _context.SaveChangesAsync();
    }
}
