using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Clases.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/clases-particulares")]
public class ClasesParticularesController : CrudController
{
    private readonly IClaseParticularService _claseParticularService;

    public ClasesParticularesController(IClaseParticularService claseParticularService) : base(claseParticularService)
    {
        _claseParticularService = claseParticularService;
    }

    [HttpPost("{id}/cancelar")]
    public async Task<ActionResult<ClaseParticularCanceladaDto>> Cancelar(string id)
    {
        return Ok(await _claseParticularService.CancelarAsync(id, User.ComoUsuarioActual()));
    }
}
