using TenisAhora.Application.Auth.Dtos;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Enums;
using TenisAhora.Domain.Exceptions;


namespace TenisAhora.Application.Auth.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IRepositorio<Profesor> _profesorRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IRepositorio<Profesor> profesorRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _usuarioRepository = usuarioRepository;
        _profesorRepository = profesorRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> RegistrarAsync(RegistrarUsuarioDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = dto.Password ?? string.Empty;
        if (email == string.Empty || password.Length < 4)
        {
            throw new DatosInvalidosException("Email y contraseña (mínimo 4 caracteres) son obligatorios.");
        }

        Usuario? usuario = await _usuarioRepository.ObtenerPorEmailAsync(email);

        if (usuario != null)
        {
            throw new EmailYaRegistradoException();
        }

        usuario = new Usuario
        {
            Id = $"usr-{Formato.Milisegundos()}",
            Nombre = (dto.Nombre ?? string.Empty).Trim(),
            Apellido = (dto.Apellido ?? string.Empty).Trim(),
            Dni = dto.Dni,
            Direccion = dto.Direccion,
            PasswordHash = _passwordHasher.Hash(password),
            Email = email,
            NumeroTelefono = dto.NumeroTelefono,
            Rol = Rol.Socio,
            NumeroSocio = $"TA-{1000 + Formato.Milisegundos() % 9000}",
            SocioDesde = Formato.Hoy()
        };

        await _usuarioRepository.AgregarAsync(usuario);

        return await GenerarRespuestaAsync(usuario);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        Usuario? usuario = await _usuarioRepository.ObtenerPorEmailAsync((dto.Email ?? string.Empty).Trim().ToLowerInvariant());

        if (usuario is null || usuario.PasswordHash == string.Empty)
        {
            throw new CredencialesInvalidasException();
        }

        if (!_passwordHasher.Verificar(dto.Password ?? string.Empty, usuario.PasswordHash))
        {
            throw new CredencialesInvalidasException();
        }

        return await GenerarRespuestaAsync(usuario);
    }

    private async Task<AuthResponseDto> GenerarRespuestaAsync(Usuario usuario)
    {
        var rol = await RolDelTokenAsync(usuario);
        var (token, expiraEn) = _jwtTokenGenerator.GenerarToken(usuario, rol);

        return new AuthResponseDto(token, usuario.Id, usuario.Nombre, usuario.Apellido, usuario.Email,
            usuario.NumeroTelefono ?? string.Empty, usuario.Direccion ?? string.Empty, rol, usuario.Dni ?? string.Empty,
            usuario.ProfesorId ?? string.Empty, usuario.NumeroSocio, usuario.Foto ?? string.Empty, expiraEn);
    }

    // El rol de un profesional sale del tipo de su ficha: Profesor o Entrenador.
    private async Task<string> RolDelTokenAsync(Usuario usuario)
    {
        if (usuario.Rol == Rol.Empleado)
            return UsuarioActual.RolEmpleado;
        if (usuario.Rol != Rol.Profesional)
            return UsuarioActual.RolSocio;

        var profesor = await _profesorRepository.ObtenerAsync(usuario.ProfesorId ?? string.Empty)
            ?? throw new NoEncontradoException($"No existe profesores/{usuario.ProfesorId}.");
        return profesor.Tipo == UsuarioActual.RolEntrenador ? UsuarioActual.RolEntrenador : UsuarioActual.RolProfesor;
    }
}
