using TenisAhora.Application.Reservas.Interfaces;
using TenisAhora.Application.Reservas.Services;

namespace TenisAhora.API.Extensions;

public static class ReservasModuleExtensions
{
    public static IServiceCollection AddReservasModule(this IServiceCollection services)
    {
        services.AddScoped<IReservaService, ReservaService>();

        return services;
    }
}
