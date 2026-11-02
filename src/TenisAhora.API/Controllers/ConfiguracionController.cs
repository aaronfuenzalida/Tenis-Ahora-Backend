using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.API.Controllers;

// Plazos de reserva y cancelación fijados por el club: solo consulta.
[ApiController]
[Route("api/configuracion")]
[Authorize]
public class ConfiguracionController : ControllerBase
{
    [HttpGet]
    public ActionResult<ConfiguracionClub> Obtener()
    {
        return Ok(ConfiguracionClub.Vigente);
    }
}
