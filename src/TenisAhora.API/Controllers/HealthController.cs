using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Administracion.Interfaces;

namespace TenisAhora.API.Controllers;

// Público: estado de la API y cantidad de registros por colección.
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IEstadoBase _estadoBase;

    public HealthController(IEstadoBase estadoBase)
    {
        _estadoBase = estadoBase;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener()
    {
        return Ok(new
        {
            status = "ok",
            service = "tenis-ahora-api",
            version = "2.0.0",
            sqlite = await _estadoBase.VersionMotorAsync(),
            uptimeSeconds = (long)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds,
            collections = await _estadoBase.ContarPorColeccionAsync()
        });
    }
}
