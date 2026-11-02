using Microsoft.EntityFrameworkCore;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

public class TenisAhoraDbContext : DbContext
{
    public TenisAhoraDbContext(DbContextOptions<TenisAhoraDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Cancha> Canchas => Set<Cancha>();
    public DbSet<TipoCancha> TiposCancha => Set<TipoCancha>();
    public DbSet<ItemStock> Stock => Set<ItemStock>();
    public DbSet<Reserva> Reservas => Set<Reserva>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<Torneo> Torneos => Set<Torneo>();
    public DbSet<Liga> Ligas => Set<Liga>();
    public DbSet<TipoCompetencia> TiposCompetencia => Set<TipoCompetencia>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Clase> Clases => Set<Clase>();
    public DbSet<ClaseParticular> ClasesParticulares => Set<ClaseParticular>();
    public DbSet<Recibo> Recibos => Set<Recibo>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<Reembolso> Reembolsos => Set<Reembolso>();
    public DbSet<Descuento> Descuentos => Set<Descuento>();
    public DbSet<Documento> Documentos => Set<Documento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Busca y aplica TODAS las IEntityTypeConfiguration de este proyecto automaticamente
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenisAhoraDbContext).Assembly);
    }
}
