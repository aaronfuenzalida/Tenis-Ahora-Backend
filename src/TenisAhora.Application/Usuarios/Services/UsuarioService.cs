using System.Text.Json.Nodes;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Usuarios.Dtos;
using TenisAhora.Application.Usuarios.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Enums;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Usuarios.Services;

public class UsuarioService : ServicioCrud<Usuario>, IUsuarioService
{
    private static readonly string[] CamposDelPerfil = ["name", "nombre", "apellido", "phone", "photo"];

    private readonly IRepositorio<Profesor> _profesorRepository;
    private readonly IRepositorio<Descuento> _descuentoRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IRepositorio<Profesor> profesorRepository,
        IRepositorio<Descuento> descuentoRepository,
        IPasswordHasher passwordHasher) : base(usuarioRepository)
    {
        _profesorRepository = profesorRepository;
        _descuentoRepository = descuentoRepository;
        _passwordHasher = passwordHasher;
    }

    protected override string Coleccion => "usuarios";
    protected override string PrefijoId => "usr";

    protected override void AutorizarListado(UsuarioActual usuario) => usuario.ExigirPersonal();

    protected override object? Visible(UsuarioActual usuario, Usuario entidad) => usuario.EsPersonal ? entidad : null;

    protected override async Task PrepararAltaAsync(Usuario usuario, JsonObject cuerpo, UsuarioActual actual)
    {
        usuario.Email = (usuario.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (usuario.Email == string.Empty)
            throw new DatosInvalidosException("El email es obligatorio.");

        var usuarios = await _repositorio.ListarAsync();
        if (usuarios.Any(u => u.Email.ToLowerInvariant() == usuario.Email))
            throw new EmailYaRegistradoException();

        await VerificarProfesionalAsync(usuario);
        if (usuario.NumeroSocio == string.Empty)
        {
            var empleados = usuarios.Count(u => u.Rol == Rol.Empleado);
            usuario.NumeroSocio = usuario.Rol == Rol.Empleado
                ? $"ADM-{empleados + 1:000}"
                : $"TA-{1000 + Formato.Milisegundos() % 9000}";
        }
        usuario.SocioDesde = Formato.SiVacio(usuario.SocioDesde, Formato.Hoy());
        usuario.DescuentosActivos ??= [];

        var password = Json.Texto(cuerpo, "password");
        usuario.PasswordHash = _passwordHasher.Hash(password == string.Empty ? Usuario.ContraseniaPorDefecto : password);
    }

    protected override void AutorizarCambio(UsuarioActual usuario, Usuario actual, JsonObject cambios)
    {
        if (!usuario.EsPersonal)
        {
            // Cada uno edita su perfil, pero solo nombre, teléfono y foto. El rol, el email, el DNI o
            // los descuentos los cambia el personal: intentarlo es 403, no se ignora.
            if (actual.Id != usuario.Id)
                throw AccesoDenegadoException.SoloPersonal();
            var prohibido = cambios.Select(c => c.Key).FirstOrDefault(clave => !CamposDelPerfil.Contains(clave));
            if (prohibido != null)
                throw new AccesoDenegadoException($"No podés modificar el campo \"{prohibido}\" de tu usuario; solo nombre, teléfono y foto.");
        }

        // Si llega el nombre completo sin nombre y apellido sueltos se recalculan desde él; si no, al
        // mezclar quedarían los anteriores.
        if (cambios.ContainsKey("name") && !cambios.ContainsKey("nombre") && !cambios.ContainsKey("apellido"))
        {
            var separado = new Usuario { NombreCompleto = Json.Texto(cambios, "name") };
            cambios["nombre"] = separado.Nombre;
            cambios["apellido"] = separado.Apellido;
        }
    }

    protected override async Task PrepararCambioAsync(Usuario actual, Usuario nuevo, JsonObject cambios, UsuarioActual usuario)
    {
        var password = Json.Texto(cambios, "password");
        nuevo.PasswordHash = password == string.Empty ? actual.PasswordHash : _passwordHasher.Hash(password);

        nuevo.Email = (nuevo.Email ?? string.Empty).Trim().ToLowerInvariant();
        var usuarios = await _repositorio.ListarAsync();
        if (usuarios.Any(u => u.Id != actual.Id && u.Email.ToLowerInvariant() == nuevo.Email))
            throw new ConflictoException("Ya existe otro usuario con ese email.");

        await VerificarProfesionalAsync(nuevo);
    }

    public async Task<Usuario> ActualizarDescuentosAsync(string id, List<string>? codigos)
    {
        var usuario = await ObtenerExistenteAsync(id);
        var catalogo = (await _descuentoRepository.ListarAsync()).Select(d => d.Codigo).ToHashSet();
        codigos ??= [];

        var desconocido = codigos.FirstOrDefault(codigo => !catalogo.Contains(codigo));
        if (desconocido != null)
            throw new DatosInvalidosException($"El descuento \"{desconocido}\" no existe en el catálogo.");

        usuario.DescuentosActivos = codigos;
        await _repositorio.ActualizarAsync(usuario);
        return usuario;
    }

    // Padrón reducido para elegir compañeros al reservar o al alumno de un turno particular.
    public async Task<List<SocioDto>> ListarSociosAsync()
    {
        var usuarios = await _repositorio.ListarAsync();
        return usuarios
            .Where(u => u.Rol == Rol.Socio)
            .Select(u => new SocioDto(u.Id, u.NombreCompleto, u.Dni, u.NumeroSocio))
            .ToList();
    }

    // Un usuario profesional tiene que apuntar a una ficha de profesional existente.
    private async Task VerificarProfesionalAsync(Usuario usuario)
    {
        if (usuario.Rol != Rol.Profesional)
        {
            usuario.ProfesorId = null;
            return;
        }
        if (string.IsNullOrEmpty(usuario.ProfesorId))
            throw new DatosInvalidosException("Un usuario profesional tiene que estar vinculado a una ficha de profesional.");
        if (!await _profesorRepository.ExisteAsync(usuario.ProfesorId))
            throw new NoEncontradoException($"No existe profesores/{usuario.ProfesorId}.");
    }
}
