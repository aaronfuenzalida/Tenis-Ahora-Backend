using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Auth.Dtos;
using TenisAhora.Application.Auth.Interfaces;

namespace TenisAhora.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("registrar")]
    public async Task<ActionResult<AuthResponseDto>> Registrar(RegistrarUsuarioDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _authService.RegistrarAsync(dto));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        return Ok(await _authService.LoginAsync(dto));
    }

    // Los claims del token, para probar la autenticación.
    [Authorize]
    [HttpGet("yo")]
    public ActionResult<Dictionary<string, string>> Yo()
    {
        return Ok(User.Claims.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.First().Value));
    }
}
