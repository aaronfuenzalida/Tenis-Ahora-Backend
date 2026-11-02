using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Administracion.Interfaces;
using TenisAhora.Application.Common;

namespace TenisAhora.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = UsuarioActual.RolEmpleado)]
public class AdminController : ControllerBase
{
    private readonly ICargadorSeed _cargadorSeed;

    public AdminController(ICargadorSeed cargadorSeed)
    {
        _cargadorSeed = cargadorSeed;
    }

    // Vacía la base y vuelve a cargar el seed.
    [HttpPost("reset")]
    public async Task<IActionResult> Resetear()
    {
        var documentos = await _cargadorSeed.ResetearAsync();
        return Ok(new { success = true, documents = documentos });
    }
}
