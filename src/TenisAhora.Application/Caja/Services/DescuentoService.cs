using System.Text.Json.Nodes;
using TenisAhora.Application.Caja.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Caja.Services;

public class DescuentoService : ServicioCrud<Descuento>, IDescuentoService
{
    private readonly IRepositorio<Usuario> _usuarioRepository;

    public DescuentoService(IRepositorio<Descuento> repositorio, IRepositorio<Usuario> usuarioRepository) : base(repositorio)
    {
        _usuarioRepository = usuarioRepository;
    }

    protected override string Coleccion => "descuentos";
    protected override string PrefijoId => "disc";

    protected override async Task PrepararAltaAsync(Descuento descuento, JsonObject cuerpo, UsuarioActual usuario)
    {
        descuento.Codigo = (descuento.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (descuento.Codigo == string.Empty)
            throw new DatosInvalidosException("El código del descuento es obligatorio.");
        if (await CodigoEnUsoAsync(descuento.Codigo, exceptoId: null))
            throw new ConflictoException("Ya existe un descuento con ese código.");

        descuento.Porcentaje = Math.Clamp(descuento.Porcentaje, 0, 100);
        descuento.AplicaA = Formato.SiVacio(descuento.AplicaA, "Alquiler de Canchas");
    }

    protected override async Task PrepararCambioAsync(Descuento actual, Descuento nuevo, JsonObject cambios, UsuarioActual usuario)
    {
        nuevo.Codigo = (nuevo.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        if (await CodigoEnUsoAsync(nuevo.Codigo, actual.Id))
            throw new ConflictoException("Ya existe otro descuento con ese código.");
        nuevo.Porcentaje = Math.Clamp(nuevo.Porcentaje, 0, 100);
    }

    // Cascada: el código se quita de los usuarios que lo tenían asignado.
    protected override async Task VerificarBajaAsync(Descuento descuento)
    {
        foreach (var usuario in await _usuarioRepository.ListarAsync())
        {
            if (!usuario.DescuentosActivos.Contains(descuento.Codigo))
                continue;
            usuario.DescuentosActivos = usuario.DescuentosActivos.Where(codigo => codigo != descuento.Codigo).ToList();
            await _usuarioRepository.ActualizarAsync(usuario);
        }
    }

    private async Task<bool> CodigoEnUsoAsync(string codigo, string? exceptoId)
    {
        var descuentos = await _repositorio.ListarAsync();
        return descuentos.Any(d => d.Id != exceptoId && d.Codigo == codigo);
    }
}
