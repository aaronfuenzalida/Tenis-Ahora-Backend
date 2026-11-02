using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Canchas.Dtos;
using TenisAhora.Application.Canchas.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

[Route("api/canchas")]
public class CanchasController : CrudController
{
    private readonly ICanchaService _canchaService;

    public CanchasController(ICanchaService canchaService) : base(canchaService)
    {
        _canchaService = canchaService;
    }

    [HttpPatch("{id}/estado")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<Cancha>> CambiarEstado(string id, EstadoCanchaDto dto)
    {
        return Ok(await _canchaService.CambiarEstadoAsync(id, dto));
    }

    [HttpPut("{id}/disponibilidad")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<Cancha>> ActualizarDisponibilidad(string id, JsonObject disponibilidad)
    {
        return Ok(await _canchaService.ActualizarDisponibilidadAsync(id, disponibilidad));
    }
}
