using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Clases.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/clases")]
public class ClasesController : CrudController
{
    private readonly IClaseService _claseService;

    public ClasesController(IClaseService claseService) : base(claseService)
    {
        _claseService = claseService;
    }

    [HttpPost("{id}/alumnos")]
    public async Task<ActionResult<AlumnoInscriptoDto>> InscribirAlumno(string id, AlumnoDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _claseService.InscribirAlumnoAsync(id, dto, User.ComoUsuarioActual()));
    }

    [HttpDelete("{id}/alumnos/{alumnoId}")]
    public async Task<IActionResult> QuitarAlumno(string id, string alumnoId)
    {
        await _claseService.QuitarAlumnoAsync(id, alumnoId, User.ComoUsuarioActual());
        return Ok(new { success = true });
    }

    [HttpPost("{id}/asistencia/alternar")]
    public async Task<ActionResult<AsistenciaActualizadaDto>> AlternarAsistencia(string id, AsistenciaDto dto)
    {
        return Ok(await _claseService.AlternarAsistenciaAsync(id, dto, User.ComoUsuarioActual()));
    }

    [HttpPost("{id}/asistencia/sesion")]
    public async Task<ActionResult<AsistenciaActualizadaDto>> MarcarSesion(string id, AsistenciaDto dto)
    {
        return Ok(await _claseService.MarcarSesionAsync(id, dto, User.ComoUsuarioActual()));
    }

    [HttpPost("{id}/asistencia/registrar")]
    public async Task<IActionResult> RegistrarAsistencia(string id, AsistenciaDto dto)
    {
        await _claseService.RegistrarAsistenciaAsync(id, dto, User.ComoUsuarioActual());
        return Ok(new { success = true });
    }
}
