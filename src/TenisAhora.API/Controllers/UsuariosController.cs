using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Common;
using TenisAhora.Application.Usuarios.Dtos;
using TenisAhora.Application.Usuarios.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

[Route("api/usuarios")]
public class UsuariosController : CrudController
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService) : base(usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpPut("{id}/descuentos")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<Usuario>> ActualizarDescuentos(string id, DescuentosUsuarioDto dto)
    {
        return Ok(await _usuarioService.ActualizarDescuentosAsync(id, dto.ActiveDiscounts));
    }

    // Padrón reducido: lo consultan socios, profesionales y personal para elegir participantes.
    [HttpGet("~/api/socios")]
    public async Task<ActionResult<List<SocioDto>>> ListarSocios()
    {
        return Ok(await _usuarioService.ListarSociosAsync());
    }
}
