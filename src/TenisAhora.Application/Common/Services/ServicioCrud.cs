using System.Text.Json.Nodes;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Common.Services;

// Base de las colecciones con alta, baja, modificación y consulta. Cada servicio agrega sus reglas
// sobreescribiendo los pasos. Por defecto cualquier usuario logueado consulta (catálogo del club) y
// solo el personal modifica. El PUT mezcla el cuerpo sobre el registro y el id no cambia.
public abstract class ServicioCrud<T> : IServicioCrud where T : class, IEntidad
{
    protected readonly IRepositorio<T> _repositorio;

    protected ServicioCrud(IRepositorio<T> repositorio)
    {
        _repositorio = repositorio;
    }

    // Nombre de la colección en la ruta, para los mensajes.
    protected abstract string Coleccion { get; }

    protected virtual string PrefijoId => Coleccion;

    public async Task<List<object>> ListarAsync(UsuarioActual usuario)
    {
        AutorizarListado(usuario);
        var entidades = await _repositorio.ListarAsync();
        return entidades.Select(e => Visible(usuario, e)).OfType<object>().ToList();
    }

    public async Task<object> ObtenerAsync(string id, UsuarioActual usuario)
    {
        var entidad = await ObtenerExistenteAsync(id);
        return Visible(usuario, entidad) ?? throw new AccesoDenegadoException("No tenés acceso a este registro.");
    }

    public async Task<object> CrearAsync(JsonObject cuerpo, UsuarioActual usuario)
    {
        AutorizarAlta(usuario, cuerpo);
        var entidad = Json.Convertir<T>(cuerpo);
        entidad.Id = await ResolverIdAsync(entidad.Id);
        await PrepararAltaAsync(entidad, cuerpo, usuario);
        await _repositorio.AgregarAsync(entidad);
        return entidad;
    }

    public async Task<object> ActualizarAsync(string id, JsonObject cambios, UsuarioActual usuario)
    {
        var actual = await ObtenerExistenteAsync(id);
        AutorizarCambio(usuario, actual, cambios);
        var nuevo = Json.Mezclar(actual, cambios);
        nuevo.Id = actual.Id;
        await PrepararCambioAsync(actual, nuevo, cambios, usuario);
        await _repositorio.ReemplazarAsync(actual, nuevo);
        return actual;
    }

    public async Task EliminarAsync(string id, UsuarioActual usuario)
    {
        var entidad = await ObtenerExistenteAsync(id);
        AutorizarBaja(usuario, entidad);
        await VerificarBajaAsync(entidad);
        await _repositorio.EliminarAsync(entidad);
    }

    protected async Task<T> ObtenerExistenteAsync(string id) =>
        await _repositorio.ObtenerAsync(id) ?? throw new NoEncontradoException($"No existe {Coleccion}/{id}.");

    protected virtual void AutorizarListado(UsuarioActual usuario) { }

    // El registro tal como lo puede ver el usuario, o null si no lo puede ver.
    protected virtual object? Visible(UsuarioActual usuario, T entidad) => entidad;

    protected virtual void AutorizarAlta(UsuarioActual usuario, JsonObject cuerpo) => usuario.ExigirPersonal();

    protected virtual async Task<string> ResolverIdAsync(string idPedido)
    {
        if (string.IsNullOrEmpty(idPedido))
            return await NuevoIdAsync();
        if (await _repositorio.ExisteAsync(idPedido))
            throw new ConflictoException($"Ya existe {Coleccion}/{idPedido}.");
        return idPedido;
    }

    protected virtual Task<string> NuevoIdAsync() => _repositorio.IdLibreAsync($"{PrefijoId}-{Formato.Milisegundos()}");

    protected virtual Task PrepararAltaAsync(T entidad, JsonObject cuerpo, UsuarioActual usuario) => Task.CompletedTask;

    // Puede sacar del cuerpo lo que el usuario no está autorizado a cambiar.
    protected virtual void AutorizarCambio(UsuarioActual usuario, T actual, JsonObject cambios) => usuario.ExigirPersonal();

    protected virtual Task PrepararCambioAsync(T actual, T nuevo, JsonObject cambios, UsuarioActual usuario) => Task.CompletedTask;

    protected virtual void AutorizarBaja(UsuarioActual usuario, T entidad) => usuario.ExigirPersonal();

    protected virtual Task VerificarBajaAsync(T entidad) => Task.CompletedTask;
}
