using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Pagos.Dtos;
using TenisAhora.Application.Pagos.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

[ApiController]
[Route("api/pagos")]
[Authorize]
public class PagosController : ControllerBase
{
    private readonly IPagoService _pagoService;

    public PagosController(IPagoService pagoService)
    {
        _pagoService = pagoService;
    }

    // 201 con la transacción aprobada; 402 si la pasarela la rechaza.
    [HttpPost]
    public async Task<ActionResult<Pago>> Crear(CrearPagoDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _pagoService.CrearAsync(dto, User.ComoUsuarioActual()));
    }
}
