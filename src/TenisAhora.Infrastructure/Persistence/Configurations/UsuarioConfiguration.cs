using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence.Configurations;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nombre).HasMaxLength(100);
        builder.Property(u => u.Apellido).HasMaxLength(100);
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.NumeroTelefono).HasMaxLength(30);
        builder.Property(u => u.Direccion).HasMaxLength(200);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Rol).HasConversion<string>().HasMaxLength(20);

        builder.HasQueryFilter(u => u.FechaBaja == null);
    }
}