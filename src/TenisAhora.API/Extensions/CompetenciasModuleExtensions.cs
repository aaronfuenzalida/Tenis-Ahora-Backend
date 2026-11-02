using TenisAhora.Application.Competencias.Interfaces;
using TenisAhora.Application.Competencias.Services;

namespace TenisAhora.API.Extensions;

public static class CompetenciasModuleExtensions
{
    public static IServiceCollection AddCompetenciasModule(this IServiceCollection services)
    {
        services.AddScoped<ITorneoService, TorneoService>();
        services.AddScoped<ILigaService, LigaService>();
        services.AddScoped<ITipoCompetenciaService, TipoCompetenciaService>();

        return services;
    }
}
