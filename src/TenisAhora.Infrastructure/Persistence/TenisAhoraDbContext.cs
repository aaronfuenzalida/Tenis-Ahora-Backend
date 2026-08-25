using Microsoft.EntityFrameworkCore;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

public class TenisAhoraDbContext : DbContext
{
    public TenisAhoraDbContext(DbContextOptions<TenisAhoraDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Busca y aplica TODAS las IEntityTypeConfiguration de este proyecto automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenisAhoraDbContext).Assembly);
    }
}