using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.API.Extensions;
using TenisAhora.Application.Common;
using TenisAhora.Application.Profesores.Dtos;
using TenisAhora.Application.Profesores.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

[Route("api/profesores")]
public class ProfesoresController : CrudController
{
    private readonly IProfesorService _profesorService;

    public ProfesoresController(IProfesorService profesorService) : base(profesorService)
    {
        _profesorService = profesorService;
    }

    [HttpPut("{id}/disponibilidad")]
    public async Task<ActionResult<Profesor>> ActualizarDisponibilidad(string id, DisponibilidadProfesorDto dto)
    {
        return Ok(await _profesorService.ActualizarDisponibilidadAsync(id, dto.AvailableHours, User.ComoUsuarioActual()));
    }

    // Campos de licenseDetails a modificar; document: { id } para el título.
    [HttpPut("{id}/certificacion")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<Profesor>> ActualizarCertificacion(string id, JsonObject cambios)
    {
        return Ok(await _profesorService.ActualizarCertificacionAsync(id, cambios));
    }
}
