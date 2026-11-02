using TenisAhora.Application.Canchas.Interfaces;
using TenisAhora.Application.Canchas.Services;

namespace TenisAhora.API.Extensions;

public static class CanchasModuleExtensions
{
    public static IServiceCollection AddCanchasModule(this IServiceCollection services)
    {
        services.AddScoped<ICanchaService, CanchaService>();
        services.AddScoped<ITipoCanchaService, TipoCanchaService>();

        return services;
    }
}
