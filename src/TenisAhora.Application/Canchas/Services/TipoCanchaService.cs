using System.Text.Json.Nodes;
using TenisAhora.Application.Canchas.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Canchas.Services;

public class TipoCanchaService : ServicioCrud<TipoCancha>, ITipoCanchaService
{
    private readonly IRepositorio<Cancha> _canchaRepository;

    public TipoCanchaService(IRepositorio<TipoCancha> repositorio, IRepositorio<Cancha> canchaRepository) : base(repositorio)
    {
        _canchaRepository = canchaRepository;
    }

    protected override string Coleccion => "tipos-cancha";
    protected override string PrefijoId => "ct";

    protected override async Task PrepararAltaAsync(TipoCancha tipo, JsonObject cuerpo, UsuarioActual usuario)
    {
        var nombre = (tipo.Nombre ?? string.Empty).Trim();
        if (nombre == string.Empty)
            throw new DatosInvalidosException("El nombre del tipo de cancha es obligatorio.");
        if (await NombreEnUsoAsync(nombre, exceptoId: null))
            throw new ConflictoException("Ya existe un tipo de cancha con ese nombre.");

        tipo.Nombre = nombre;
        tipo.Etiqueta = Formato.SiVacio(tipo.Etiqueta, nombre);
        tipo.Descripcion ??= string.Empty;
        tipo.Velocidad = Formato.SiVacio(tipo.Velocidad, "Media");
        tipo.FrecuenciaMantenimiento ??= string.Empty;
        tipo.MaximoAlumnosPorClase = TipoCancha.LimitarCupo(tipo.MaximoAlumnosPorClase);
    }

    protected override async Task PrepararCambioAsync(TipoCancha actual, TipoCancha nuevo, JsonObject cambios, UsuarioActual usuario)
    {
        if (await NombreEnUsoAsync((nuevo.Nombre ?? string.Empty).Trim(), actual.Id))
            throw new ConflictoException("Ya existe otro tipo de cancha con ese nombre.");
        nuevo.MaximoAlumnosPorClase = TipoCancha.LimitarCupo(nuevo.MaximoAlumnosPorClase);
    }

    protected override async Task VerificarBajaAsync(TipoCancha tipo)
    {
        var canchas = await _canchaRepository.ListarAsync();
        if (canchas.Any(c => c.TipoSuperficie == tipo.Nombre))
            throw new ConflictoException($"No se puede eliminar: existen canchas registradas con superficie \"{tipo.Nombre}\".");
    }

    private async Task<bool> NombreEnUsoAsync(string nombre, string? exceptoId)
    {
        var tipos = await _repositorio.ListarAsync();
        return tipos.Any(t => t.Id != exceptoId && string.Equals(t.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
    }
}
