using System.Text.Json;
using System.Text.Json.Nodes;
using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Clases.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Profesores.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Clases.Services;

public class ClaseService : ServicioCrud<Clase>, IClaseService
{
    private readonly IRepositorio<TipoCancha> _tipoCanchaRepository;
    private readonly IProfesorService _profesorService;

    public ClaseService(IRepositorio<Clase> repositorio, IRepositorio<TipoCancha> tipoCanchaRepository, IProfesorService profesorService) : base(repositorio)
    {
        _tipoCanchaRepository = tipoCanchaRepository;
        _profesorService = profesorService;
    }

    protected override string Coleccion => "clases";
    protected override string PrefijoId => "cls";

    protected override object? Visible(UsuarioActual usuario, Clase clase)
    {
        if (usuario.EsPersonal)
            return clase;
        if (usuario.EsProfesional)
            return usuario.Dicta(clase.ProfesorId) ? clase : null;

        // Un socio ve la oferta y el cupo, no los datos de los demás alumnos; solo si él está inscripto.
        var vista = JsonSerializer.SerializeToNode(clase, JsonSerializerOptions.Web)!.AsObject();
        vista.Remove("students");
        vista["enrolled"] = usuario.Dni != string.Empty
            && clase.Alumnos.Any(a => Formato.SoloDigitos(a.Dni) == Formato.SoloDigitos(usuario.Dni));
        return vista;
    }

    protected override async Task PrepararAltaAsync(Clase clase, JsonObject cuerpo, UsuarioActual usuario)
    {
        clase.Clasificacion = Formato.SiVacio(clase.Clasificacion, "clase");
        clase.Tipo = Formato.SiVacio(clase.Tipo, "grupal");
        await VerificarCupoAsync(clase);
        clase.Alumnos ??= [];
        await _profesorService.VerificarTipoDeClaseAsync(clase.ProfesorId, clase.Clasificacion);
        await _profesorService.VerificarHabilitadoAsync(clase.ProfesorId);
    }

    protected override async Task PrepararCambioAsync(Clase actual, Clase nueva, JsonObject cambios, UsuarioActual usuario)
    {
        await VerificarCupoAsync(nueva);
        await _profesorService.VerificarTipoDeClaseAsync(nueva.ProfesorId, nueva.Clasificacion);
        // Solo si cambia el profesor, para que una clase ya asignada siga editable.
        if (nueva.ProfesorId != actual.ProfesorId)
            await _profesorService.VerificarHabilitadoAsync(nueva.ProfesorId);
    }

    // RD07 / RD10: además del personal y del profesional a cargo, un socio se puede anotar a sí mismo.
    public async Task<AlumnoInscriptoDto> InscribirAlumnoAsync(string id, AlumnoDto dto, UsuarioActual usuario)
    {
        Clase clase;
        if (usuario.EsSocio)
        {
            if (!usuario.EsSuDni(dto.Dni))
                throw new AccesoDenegadoException("Solo podés inscribirte a vos mismo.");
            clase = await ObtenerExistenteAsync(id);
        }
        else
        {
            clase = await ObtenerACargoAsync(id, usuario);
        }

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new DatosInvalidosException("El nombre del alumno es obligatorio.");
        var alumnos = clase.Alumnos ?? [];
        var dni = Formato.SoloDigitos(dto.Dni);
        if (dni != string.Empty && alumnos.Any(a => Formato.SoloDigitos(a.Dni) == dni))
            throw new ConflictoException($"Ya hay un alumno inscripto en \"{clase.Nombre}\" con ese DNI.");
        var cupo = TipoCancha.LimitarCupo(clase.CupoMaximo);
        if (alumnos.Count >= cupo)
            throw new ConflictoException($"No es posible agregar más alumnos: el cupo de {cupo} está completo.");

        var alumno = new Alumno
        {
            Id = $"std-{Formato.Milisegundos()}",
            Nombre = dto.Name,
            Dni = dto.Dni ?? string.Empty,
            Telefono = dto.Phone ?? string.Empty,
            InscriptoEl = Formato.Hoy(),
            Asistencia = Enumerable.Repeat("P", Math.Max((clase.Sesiones ?? []).Count, 1)).ToList()
        };
        alumnos.Add(alumno);
        clase.Alumnos = alumnos;
        clase.Inscriptos = alumnos.Count;
        await _repositorio.ActualizarAsync(clase);
        return new AlumnoInscriptoDto(true, alumno);
    }

    public async Task QuitarAlumnoAsync(string id, string alumnoId, UsuarioActual usuario)
    {
        var clase = await ObtenerACargoAsync(id, usuario);
        clase.Alumnos = (clase.Alumnos ?? []).Where(a => a.Id != alumnoId).ToList();
        clase.Inscriptos = clase.Alumnos.Count;
        await _repositorio.ActualizarAsync(clase);
    }

    // P -> A -> P.
    public async Task<AsistenciaActualizadaDto> AlternarAsistenciaAsync(string id, AsistenciaDto dto, UsuarioActual usuario)
    {
        var clase = await ObtenerACargoAsync(id, usuario);
        var sesion = Math.Max(dto.SessionIndex ?? 0, 0);
        foreach (var alumno in (clase.Alumnos ?? []).Where(a => a.Id == dto.StudentId))
        {
            var asistencia = CompletarHasta(alumno.Asistencia, sesion);
            asistencia[sesion] = asistencia[sesion] == "P" ? "A" : "P";
            alumno.Asistencia = asistencia;
        }
        await _repositorio.ActualizarAsync(clase);
        return await AsistenciaActualizadaAsync(usuario);
    }

    public async Task<AsistenciaActualizadaDto> MarcarSesionAsync(string id, AsistenciaDto dto, UsuarioActual usuario)
    {
        var clase = await ObtenerACargoAsync(id, usuario);
        var sesion = Math.Max(dto.SessionIndex ?? 0, 0);
        var estado = Formato.SiVacio(dto.Status, "P");
        VerificarEstado(estado);
        foreach (var alumno in clase.Alumnos ?? [])
        {
            var asistencia = CompletarHasta(alumno.Asistencia, sesion);
            asistencia[sesion] = estado;
            alumno.Asistencia = asistencia;
        }
        await _repositorio.ActualizarAsync(clase);
        return await AsistenciaActualizadaAsync(usuario);
    }

    // Agrega una sesión al alumno.
    public async Task RegistrarAsistenciaAsync(string id, AsistenciaDto dto, UsuarioActual usuario)
    {
        var clase = await ObtenerACargoAsync(id, usuario);
        var estado = dto.Status ?? string.Empty;
        VerificarEstado(estado);
        foreach (var alumno in (clase.Alumnos ?? []).Where(a => a.Id == dto.StudentId))
            alumno.Asistencia = [.. (alumno.Asistencia ?? []), estado];
        await _repositorio.ActualizarAsync(clase);
    }

    // El personal o el profesional a cargo de la clase.
    private async Task<Clase> ObtenerACargoAsync(string id, UsuarioActual usuario)
    {
        var clase = await ObtenerExistenteAsync(id);
        if (!usuario.EsPersonal && !usuario.Dicta(clase.ProfesorId))
            throw new AccesoDenegadoException("La clase no está a tu cargo.");
        return clase;
    }

    private async Task<AsistenciaActualizadaDto> AsistenciaActualizadaAsync(UsuarioActual usuario) =>
        new(true, await ListarAsync(usuario));

    // RD07 (tope de 30) y RD10 (cupo del tipo de cancha de la comisión). Si no se reconoce la
    // superficie queda solo el tope de 30; sin cupo informado se usa el máximo permitido.
    private async Task VerificarCupoAsync(Clase clase)
    {
        var limite = TipoCancha.CupoMaximo;
        var cancha = clase.CanchaAsignada ?? string.Empty;
        var apertura = cancha.LastIndexOf('(');
        var cierre = cancha.LastIndexOf(')');
        if (apertura >= 0 && cierre > apertura)
        {
            var superficie = cancha[(apertura + 1)..cierre].Trim();
            var tipos = await _tipoCanchaRepository.ListarAsync();
            var tipo = tipos.FirstOrDefault(t => string.Equals(t.Nombre, superficie, StringComparison.OrdinalIgnoreCase));
            if (tipo != null)
                limite = TipoCancha.LimitarCupo(tipo.MaximoAlumnosPorClase);
        }

        if (clase.CupoMaximo > TipoCancha.CupoMaximo)
            throw new DatosInvalidosException("El cupo reglamentario máximo es de hasta 30 alumnos.");
        if (clase.CupoMaximo > limite)
            throw new DatosInvalidosException($"El cupo máximo para {cancha} es de {limite} alumnos (RD10).");
        if (clase.CupoMaximo <= 0)
            clase.CupoMaximo = limite;
    }

    private static void VerificarEstado(string estado)
    {
        if (estado != "P" && estado != "A")
            throw new DatosInvalidosException("El estado de asistencia debe ser P (presente) o A (ausente).");
    }

    // Las sesiones sin marcar quedan con "-".
    private static List<string> CompletarHasta(List<string>? asistencia, int sesion)
    {
        var completa = asistencia ?? [];
        while (completa.Count <= sesion)
            completa.Add("-");
        return completa;
    }
}
