using System.Globalization;
using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Application.Profesores.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Profesores.Services;

public class ProfesorService : ServicioCrud<Profesor>, IProfesorService
{
    // El tipo y la certificación los valida el club, no el profesional.
    private static readonly string[] CamposQueValidaElClub = ["type", "licenseDetails", "hasOfficialLicense"];

    private readonly IDocumentoService _documentoService;

    public ProfesorService(IRepositorio<Profesor> repositorio, IDocumentoService documentoService) : base(repositorio)
    {
        _documentoService = documentoService;
    }

    protected override string Coleccion => "profesores";
    protected override string PrefijoId => "coach";

    protected override async Task PrepararAltaAsync(Profesor profesor, JsonObject cuerpo, UsuarioActual usuario)
    {
        await NormalizarTituloAsync(profesor);
        profesor.Tipo = Formato.SiVacio(profesor.Tipo, "Profesor");
    }

    // Un profesional edita su propia ficha, salvo el tipo y la certificación.
    protected override void AutorizarCambio(UsuarioActual usuario, Profesor actual, JsonObject cambios)
    {
        if (usuario.EsPersonal)
            return;
        if (!usuario.EsProfesional || actual.Id != usuario.ProfesorId)
            throw AccesoDenegadoException.SoloPersonal();
        foreach (var campo in CamposQueValidaElClub)
            cambios.Remove(campo);
    }

    // El título se revalida solo si viene en el cambio: editar horarios o datos de contacto no exige
    // volver a presentarlo.
    protected override async Task PrepararCambioAsync(Profesor actual, Profesor nuevo, JsonObject cambios, UsuarioActual usuario)
    {
        if (cambios.ContainsKey("licenseDetails"))
            await NormalizarTituloAsync(nuevo);
    }

    public async Task<Profesor> ActualizarDisponibilidadAsync(string id, string? horarios, UsuarioActual usuario)
    {
        if (!usuario.EsPersonal && !(usuario.EsProfesional && id == usuario.ProfesorId))
            throw AccesoDenegadoException.SoloPersonal();

        var profesor = await ObtenerExistenteAsync(id);
        profesor.HorariosDisponibles = horarios ?? string.Empty;
        await _repositorio.ActualizarAsync(profesor);
        return profesor;
    }

    public async Task<Profesor> ActualizarCertificacionAsync(string id, JsonObject cambios)
    {
        var profesor = await ObtenerExistenteAsync(id);
        profesor.Titulo = Json.Mezclar(profesor.Titulo ?? new TituloHabilitante(), cambios);
        await NormalizarTituloAsync(profesor);
        await _repositorio.ActualizarAsync(profesor);
        return profesor;
    }

    public async Task VerificarTipoDeClaseAsync(string? profesorId, string? tipoDeClase)
    {
        var profesor = await _repositorio.ObtenerAsync(profesorId ?? string.Empty);
        if (profesor == null)
            return;
        var permitido = Profesor.TipoDeClaseSegun(profesor.Tipo);
        if (tipoDeClase != permitido)
            throw new DatosInvalidosException($"{profesor.Nombre} es {profesor.Tipo.ToLowerInvariant()}: solo puede tener {permitido}s.");
    }

    public async Task VerificarHabilitadoAsync(string? profesorId)
    {
        var profesor = await _repositorio.ObtenerAsync(profesorId ?? string.Empty);
        profesor?.VerificarHabilitado(Formato.Hoy());
    }

    // RD08: el título habilitante, con su documento adjunto, es obligatorio y no se completa con datos
    // inventados.
    private async Task NormalizarTituloAsync(Profesor profesor)
    {
        var titulo = profesor.Titulo ?? new TituloHabilitante();
        (string Valor, string Etiqueta)[] obligatorios =
        [
            (titulo.Denominacion, "la denominación del título"),
            (titulo.Institucion, "la institución emisora"),
            (titulo.NumeroLicencia, "el número de matrícula o licencia"),
            (titulo.Vencimiento, "la fecha de vencimiento"),
        ];
        foreach (var (valor, etiqueta) in obligatorios)
        {
            if (string.IsNullOrWhiteSpace(valor))
                throw new DatosInvalidosException($"Falta {etiqueta} del título habilitante (RD08).");
        }
        if (!DateTime.TryParseExact(titulo.Vencimiento, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new DatosInvalidosException("La fecha de vencimiento del título no es válida (formato AAAA-MM-DD).");
        if (string.IsNullOrEmpty(titulo.Documento?.Id))
            throw new DatosInvalidosException("Falta adjuntar el documento del título habilitante (RD08).");

        titulo.Documento = await _documentoService.ResolverReferenciaAsync(titulo.Documento.Id);
        titulo.Estado = Formato.SiVacio(titulo.Estado, "habilitado");
        profesor.Titulo = titulo;
        profesor.TieneLicenciaOficial = true;
    }
}
