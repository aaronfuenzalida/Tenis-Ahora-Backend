using System.Security.Claims;
using TenisAhora.Application.Common;

namespace TenisAhora.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    // Los claims llegan con el nombre que tienen en el token (MapInboundClaims = false).
    public static UsuarioActual ComoUsuarioActual(this ClaimsPrincipal principal) => new(
        Leer(principal, "sub"),
        Leer(principal, "email"),
        Leer(principal, "role"),
        Leer(principal, "dni"),
        Leer(principal, "coachId"));

    private static string Leer(ClaimsPrincipal principal, string tipo) => principal.FindFirst(tipo)?.Value ?? string.Empty;
}
