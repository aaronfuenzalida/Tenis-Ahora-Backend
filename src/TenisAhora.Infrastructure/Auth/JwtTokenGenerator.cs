using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, long ExpiraEn) GenerarToken(Usuario usuario, string rol)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
            new Claim("role", rol),
        };

        // El DNI y la ficha del profesional deciden qué registros son de cada usuario.
        if (!string.IsNullOrEmpty(usuario.Dni))
            claims.Add(new Claim("dni", usuario.Dni));
        if (!string.IsNullOrEmpty(usuario.ProfesorId))
            claims.Add(new Claim("coachId", usuario.ProfesorId));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddMinutes(_settings.ExpiracionMinutos);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), new DateTimeOffset(expira).ToUnixTimeSeconds());
    }
}
