using TenisAhora.Application.Administracion.Interfaces;
using TenisAhora.Infrastructure.Persistence;

namespace TenisAhora.API.Extensions;

public static class AdministracionModuleExtensions
{
    public static IServiceCollection AddAdministracionModule(this IServiceCollection services, string rutaSeed)
    {
        services.Configure<SeedSettings>(settings => settings.Ruta = rutaSeed);

        services.AddScoped<IEstadoBase, EstadoBase>();
        services.AddScoped<ICargadorSeed, CargadorSeed>();

        return services;
    }
}
