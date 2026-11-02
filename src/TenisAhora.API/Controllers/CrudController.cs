using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Common.Interfaces;

namespace TenisAhora.API.Controllers;

// Rutas comunes de cada colección. Los permisos por rol y por dueño del registro los aplica el servicio.
[ApiController]
[Authorize]
public abstract class CrudController : ControllerBase
{
    private readonly IServicioCrud _servicio;

    protected CrudController(IServicioCrud servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> Listar()
    {
        return Ok(await _servicio.ListarAsync(User.ComoUsuarioActual()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Obtener(string id)
    {
        return Ok(await _servicio.ObtenerAsync(id, User.ComoUsuarioActual()));
    }

    [HttpPost]
    public async Task<ActionResult<object>> Crear(JsonObject cuerpo)
    {
        return StatusCode(StatusCodes.Status201Created, await _servicio.CrearAsync(cuerpo, User.ComoUsuarioActual()));
    }

    // Mezcla el cuerpo sobre el registro: los campos que no vienen quedan como estaban.
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> Actualizar(string id, JsonObject cambios)
    {
        return Ok(await _servicio.ActualizarAsync(id, cambios, User.ComoUsuarioActual()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        await _servicio.EliminarAsync(id, User.ComoUsuarioActual());
        return Ok(new { success = true });
    }
}
