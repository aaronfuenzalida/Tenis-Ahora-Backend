using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Application.Auth.Services;
using TenisAhora.Infrastructure.Auth;
using TenisAhora.Infrastructure.Persistence;

namespace TenisAhora.API.Extensions;

public static class AuthModuleExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}