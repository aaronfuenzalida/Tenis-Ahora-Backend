using TenisAhora.Application.Auth.Dtos;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Enums;
using TenisAhora.Domain.Exceptions;


namespace TenisAhora.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> RegistrarAsync(RegistrarUsuarioDto dto)
    {
        Usuario? usuario = await _usuarioRepository.ObtenerPorEmailAsync(dto.Email);

        if (usuario != null)
        {
            throw new EmailYaRegistradoException(dto.Email);
        }

        usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Apellido = dto.Apellido,
            Direccion = dto.Direccion,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            Email = dto.Email,
            NumeroTelefono = dto.NumeroTelefono,
            Rol = Rol.Socio
        };

        await _usuarioRepository.AgregarAsync(usuario);

        return new AuthResponseDto(_jwtTokenGenerator.GenerarToken(usuario), usuario.Email, usuario.Rol.ToString());
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        Usuario? usuario = await _usuarioRepository.ObtenerPorEmailAsync(dto.Email);

        if (usuario is null || !usuario.EstaActivo)
        {
            throw new CredencialesInvalidasException();
        }

        if (!_passwordHasher.Verificar(dto.Password, usuario.PasswordHash))
        {
            throw new CredencialesInvalidasException();
        }

        return new AuthResponseDto(_jwtTokenGenerator.GenerarToken(usuario), usuario.Email, usuario.Rol.ToString());
    }
}