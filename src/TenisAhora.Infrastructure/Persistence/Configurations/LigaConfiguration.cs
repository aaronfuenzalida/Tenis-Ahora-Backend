using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence.Configurations;

public class LigaConfiguration : IEntityTypeConfiguration<Liga>
{
    public void Configure(EntityTypeBuilder<Liga> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.DocumentoReglamento).ComoJson();
        builder.Property(l => l.Participantes).ComoJson();
        builder.Property(l => l.Posiciones).ComoJson();
        builder.Property(l => l.Fixture).ComoJson();
    }
}
