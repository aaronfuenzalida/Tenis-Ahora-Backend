using TenisAhora.Application.Usuarios.Interfaces;
using TenisAhora.Application.Usuarios.Services;

namespace TenisAhora.API.Extensions;

public static class UsuariosModuleExtensions
{
    public static IServiceCollection AddUsuariosModule(this IServiceCollection services)
    {
        services.AddScoped<IUsuarioService, UsuarioService>();

        return services;
    }
}
