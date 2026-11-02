using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence.Configurations;

public class TorneoConfiguration : IEntityTypeConfiguration<Torneo>
{
    public void Configure(EntityTypeBuilder<Torneo> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.DocumentoReglamento).ComoJson();
        builder.Property(t => t.Participantes).ComoJson();
        builder.Property(t => t.Cuadro).ComoJson();
    }
}
