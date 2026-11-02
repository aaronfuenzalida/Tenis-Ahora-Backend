using TenisAhora.Application.Caja.Interfaces;
using TenisAhora.Application.Caja.Services;

namespace TenisAhora.API.Extensions;

public static class CajaModuleExtensions
{
    public static IServiceCollection AddCajaModule(this IServiceCollection services)
    {
        services.AddScoped<IReciboService, ReciboService>();
        services.AddScoped<IFacturaService, FacturaService>();
        services.AddScoped<IReembolsoService, ReembolsoService>();
        services.AddScoped<IDescuentoService, DescuentoService>();

        return services;
    }
}
