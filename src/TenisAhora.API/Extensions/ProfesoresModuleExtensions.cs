using TenisAhora.Application.Profesores.Interfaces;
using TenisAhora.Application.Profesores.Services;

namespace TenisAhora.API.Extensions;

public static class ProfesoresModuleExtensions
{
    public static IServiceCollection AddProfesoresModule(this IServiceCollection services)
    {
        services.AddScoped<IProfesorService, ProfesorService>();

        return services;
    }
}
