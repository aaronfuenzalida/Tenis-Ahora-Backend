using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence.Configurations;

public class ReservaConfiguration : IEntityTypeConfiguration<Reserva>
{
    public void Configure(EntityTypeBuilder<Reserva> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Participantes).ComoJson();
        builder.Property(r => r.Equipamiento).ComoJson();
        builder.Property(r => r.EquipamientoTomado).ComoJson();
        builder.Property(r => r.Cancelacion).ComoJson();
    }
}
