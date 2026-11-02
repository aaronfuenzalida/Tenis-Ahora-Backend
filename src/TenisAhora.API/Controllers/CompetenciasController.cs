using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Common;
using TenisAhora.Application.Competencias.Dtos;
using TenisAhora.Application.Competencias.Interfaces;

namespace TenisAhora.API.Controllers;

// Torneos y ligas comparten las rutas de reglamento, inscripciones y enfrentamientos.
public abstract class CompetenciasController : CrudController
{
    private readonly IServicioCompetencia _competenciaService;

    protected CompetenciasController(IServicioCompetencia competenciaService) : base(competenciaService)
    {
        _competenciaService = competenciaService;
    }

    // internalRulesDocument: { id } de un documento subido; null lo quita.
    [HttpPut("{id}/reglamento")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<object>> ActualizarReglamento(string id, JsonObject cuerpo)
    {
        return Ok(await _competenciaService.ActualizarReglamentoAsync(id, cuerpo));
    }

    [HttpPost("{id}/inscripciones")]
    public async Task<ActionResult<InscripcionRealizadaDto>> Inscribir(string id, InscripcionDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _competenciaService.InscribirAsync(id, dto, User.ComoUsuarioActual()));
    }

    [HttpDelete("{id}/inscripciones/{participanteId}")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<IActionResult> Desinscribir(string id, string participanteId)
    {
        await _competenciaService.DesinscribirAsync(id, participanteId);
        return Ok(new { success = true });
    }

    [HttpPost("{id}/enfrentamientos")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<EnfrentamientoGuardadoDto>> AgregarEnfrentamiento(string id, NuevoEnfrentamientoDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _competenciaService.AgregarEnfrentamientoAsync(id, dto));
    }

    [HttpPut("{id}/enfrentamientos/{enfrentamientoId}")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<EnfrentamientoGuardadoDto>> ActualizarEnfrentamiento(string id, string enfrentamientoId, JsonObject cambios)
    {
        return Ok(await _competenciaService.ActualizarEnfrentamientoAsync(id, enfrentamientoId, cambios));
    }
}
