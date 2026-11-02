using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Common;
using TenisAhora.Application.Reservas.Dtos;
using TenisAhora.Application.Reservas.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

// Alta pendiente de pago -> seña -> inicio del alquiler -> saldo.
[ApiController]
[Route("api/reservas")]
[Authorize]
public class ReservasController : ControllerBase
{
    private readonly IReservaService _reservaService;

    public ReservasController(IReservaService reservaService)
    {
        _reservaService = reservaService;
    }

    [HttpGet]
    public async Task<ActionResult<List<object>>> Listar()
    {
        return Ok(await _reservaService.ListarAsync(User.ComoUsuarioActual()));
    }

    [HttpGet("ocupacion")]
    public async Task<ActionResult<List<OcupacionDto>>> Ocupacion()
    {
        return Ok(await _reservaService.ListarOcupacionAsync(User.ComoUsuarioActual()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> Obtener(string id)
    {
        return Ok(await _reservaService.ObtenerAsync(id, User.ComoUsuarioActual()));
    }

    [HttpPost]
    public async Task<ActionResult<ReservaCreadaDto>> Crear(CrearReservaDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _reservaService.CrearAsync(dto, User.ComoUsuarioActual()));
    }

    // Solo cambia horario, cancha o jugadores; el estado y los cobros no se tocan por acá.
    [HttpPut("{id}")]
    public async Task<ActionResult<object>> Actualizar(string id, JsonObject cambios)
    {
        return Ok(await _reservaService.ActualizarAsync(id, cambios, User.ComoUsuarioActual()));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        await _reservaService.EliminarAsync(id, User.ComoUsuarioActual());
        return Ok(new { success = true });
    }

    [HttpPost("{id}/pagar-sena")]
    public async Task<ActionResult<CobroReservaDto>> PagarSena(string id, PagoReservaDto dto)
    {
        return Ok(await _reservaService.PagarSenaAsync(id, dto.TransactionId, User.ComoUsuarioActual()));
    }

    [HttpPost("{id}/iniciar")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<Reserva>> Iniciar(string id)
    {
        return Ok(await _reservaService.IniciarAsync(id));
    }

    [HttpPost("{id}/pagar-saldo")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<CobroReservaDto>> PagarSaldo(string id, PagoReservaDto dto)
    {
        return Ok(await _reservaService.PagarSaldoAsync(id, dto.TransactionId, User.ComoUsuarioActual()));
    }

    [HttpPost("{id}/cancelar")]
    public async Task<ActionResult<CancelacionReservaDto>> Cancelar(string id)
    {
        return Ok(await _reservaService.CancelarAsync(id, User.ComoUsuarioActual()));
    }
}
