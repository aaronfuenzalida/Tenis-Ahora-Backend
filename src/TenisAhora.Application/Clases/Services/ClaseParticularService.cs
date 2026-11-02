using System.Text.Json.Nodes;
using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Clases.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Profesores.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Clases.Services;

public class ClaseParticularService : ServicioCrud<ClaseParticular>, IClaseParticularService
{
    private readonly IProfesorService _profesorService;

    public ClaseParticularService(IRepositorio<ClaseParticular> repositorio, IProfesorService profesorService) : base(repositorio)
    {
        _profesorService = profesorService;
    }

    protected override string Coleccion => "clases-particulares";
    protected override string PrefijoId => "prv";

    protected override object? Visible(UsuarioActual usuario, ClaseParticular clase) =>
        usuario.EsPersonal || usuario.Dicta(clase.ProfesorId) || (usuario.EsSocio && usuario.EsSuDni(clase.DniAlumno)) ? clase : null;

    protected override void AutorizarAlta(UsuarioActual usuario, JsonObject cuerpo)
    {
        if (usuario.EsPersonal)
            return;
        if (usuario.EsProfesional)
        {
            // El turno queda a nombre del profesional y del tipo que puede dar, no del que mande.
            cuerpo["coachId"] = usuario.ProfesorId;
            cuerpo["kind"] = Profesor.TipoDeClaseSegun(usuario.Rol);
            return;
        }
        if (!usuario.EsSocio)
            throw AccesoDenegadoException.SoloPersonal();
        if (!usuario.EsSuDni(Json.Texto(cuerpo, "studentDni")))
            throw new AccesoDenegadoException("Solo podés pedir clases particulares para vos.");
    }

    protected override async Task PrepararAltaAsync(ClaseParticular clase, JsonObject cuerpo, UsuarioActual usuario)
    {
        clase.Clasificacion = Formato.SiVacio(clase.Clasificacion, "clase");
        clase.Estado = Formato.SiVacio(clase.Estado, "programada");
        await _profesorService.VerificarTipoDeClaseAsync(clase.ProfesorId, clase.Clasificacion);
        await _profesorService.VerificarHabilitadoAsync(clase.ProfesorId);
        await VerificarSuperposicionAsync(clase);
    }

    protected override void AutorizarCambio(UsuarioActual usuario, ClaseParticular actual, JsonObject cambios)
    {
        if (usuario.EsPersonal)
            return;
        if (!usuario.Dicta(actual.ProfesorId))
            throw AccesoDenegadoException.SoloPersonal();
        cambios["coachId"] = usuario.ProfesorId;
        cambios["kind"] = Profesor.TipoDeClaseSegun(usuario.Rol);
    }

    protected override async Task PrepararCambioAsync(ClaseParticular actual, ClaseParticular nueva, JsonObject cambios, UsuarioActual usuario)
    {
        await _profesorService.VerificarTipoDeClaseAsync(nueva.ProfesorId, nueva.Clasificacion);
        if (nueva.ProfesorId != actual.ProfesorId)
            await _profesorService.VerificarHabilitadoAsync(nueva.ProfesorId);
        await VerificarSuperposicionAsync(nueva);
    }

    protected override void AutorizarBaja(UsuarioActual usuario, ClaseParticular clase)
    {
        if (!usuario.EsPersonal && !usuario.Dicta(clase.ProfesorId))
            throw AccesoDenegadoException.SoloPersonal();
    }

    public async Task<ClaseParticularCanceladaDto> CancelarAsync(string id, UsuarioActual usuario)
    {
        var clase = await ObtenerExistenteAsync(id);
        if (Visible(usuario, clase) == null)
            throw new AccesoDenegadoException("La clase particular no es tuya.");

        clase.Estado = "cancelada";
        await _repositorio.ActualizarAsync(clase);
        return new ClaseParticularCanceladaDto(true, clase);
    }

    // Ni dos turnos el mismo día y horario para el mismo alumno, ni para el mismo profesional.
    private async Task VerificarSuperposicionAsync(ClaseParticular clase)
    {
        if (clase.Estado == "cancelada")
            return;
        var dni = Formato.SoloDigitos(clase.DniAlumno);
        foreach (var otra in await _repositorio.ListarAsync())
        {
            if (otra.Id == clase.Id || otra.Estado == "cancelada" || otra.Fecha != clase.Fecha || Turno(otra.Horario) != Turno(clase.Horario))
                continue;
            if (dni != string.Empty && Formato.SoloDigitos(otra.DniAlumno) == dni)
                throw new ConflictoException($"Ya hay un turno particular para ese alumno el {otra.Fecha} a las {otra.Horario}, con {otra.NombreProfesor}.");
            if (!string.IsNullOrEmpty(clase.ProfesorId) && otra.ProfesorId == clase.ProfesorId)
                throw new ConflictoException($"{otra.NombreProfesor} ya tiene un turno el {otra.Fecha} a las {otra.Horario}.");
        }
    }

    // "10:00 - 11:00" y "10:00-11:00" son el mismo turno.
    private static string Turno(string? horario) =>
        string.Concat((horario ?? string.Empty).Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
}
