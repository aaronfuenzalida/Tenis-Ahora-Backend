using TenisAhora.Application.Pagos.Interfaces;
using TenisAhora.Application.Pagos.Services;

namespace TenisAhora.API.Extensions;

public static class PagosModuleExtensions
{
    public static IServiceCollection AddPagosModule(this IServiceCollection services)
    {
        services.AddScoped<IPagoService, PagoService>();

        return services;
    }
}
