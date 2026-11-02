using TenisAhora.Application.Clases.Interfaces;
using TenisAhora.Application.Clases.Services;

namespace TenisAhora.API.Extensions;

public static class ClasesModuleExtensions
{
    public static IServiceCollection AddClasesModule(this IServiceCollection services)
    {
        services.AddScoped<IClaseService, ClaseService>();
        services.AddScoped<IClaseParticularService, ClaseParticularService>();

        return services;
    }
}
