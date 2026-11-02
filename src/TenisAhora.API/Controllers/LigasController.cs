using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Common;
using TenisAhora.Application.Competencias.Dtos;
using TenisAhora.Application.Competencias.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/ligas")]
public class LigasController : CompetenciasController
{
    private readonly ILigaService _ligaService;

    public LigasController(ILigaService ligaService) : base(ligaService)
    {
        _ligaService = ligaService;
    }

    [HttpPost("{id}/enfrentamientos/{enfrentamientoId}/resultado")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<ResultadoLigaDto>> RegistrarResultado(string id, string enfrentamientoId, ResultadoDto dto)
    {
        return Ok(await _ligaService.RegistrarResultadoAsync(id, enfrentamientoId, dto));
    }
}
