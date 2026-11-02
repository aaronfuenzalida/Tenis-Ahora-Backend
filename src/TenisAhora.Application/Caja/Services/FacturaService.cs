using System.Text.Json.Nodes;
using TenisAhora.Application.Caja.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Caja.Services;

public class FacturaService : ServicioCrud<Factura>, IFacturaService
{
    public FacturaService(IRepositorio<Factura> repositorio) : base(repositorio) { }

    protected override string Coleccion => "facturas";

    protected override void AutorizarListado(UsuarioActual usuario) => usuario.ExigirPersonal();

    protected override object? Visible(UsuarioActual usuario, Factura factura) => usuario.EsPersonal ? factura : null;

    protected override async Task<string> NuevoIdAsync()
    {
        var cantidad = await _repositorio.ContarAsync();
        return await _repositorio.IdLibreAsync($"FC-0001-{412 + cantidad + 1:00000000}");
    }

    // El total sale de los ítems, no de lo que mande el cliente.
    protected override Task PrepararAltaAsync(Factura factura, JsonObject cuerpo, UsuarioActual usuario)
    {
        var items = factura.Items ?? [];
        if (items.Count == 0 || items.Any(i => string.IsNullOrWhiteSpace(i.Descripcion)))
            throw new DatosInvalidosException("La factura debe tener al menos un ítem con descripción.");
        foreach (var item in items.Where(i => i.Cantidad <= 0))
            item.Cantidad = 1;
        var subtotal = items.Sum(i => i.Cantidad * i.PrecioUnitario);
        if (subtotal <= 0)
            throw new DatosInvalidosException("El importe de la factura debe ser mayor a cero.");

        factura.Concepto = Formato.SiVacio(factura.Concepto, items[0].Descripcion);
        factura.MedioPago = Formato.SiVacio(factura.MedioPago, "Tarjeta de Débito");
        factura.EmitidaPor = Formato.SiVacio(factura.EmitidaPor, "Administración General");
        if (string.IsNullOrEmpty(factura.ReciboRelacionado))
            factura.ReciboRelacionado = null;
        factura.Items = items;
        factura.Subtotal = subtotal;
        factura.Total = subtotal;
        factura.Estado = "emitida";
        factura.Fecha = Formato.MarcaHoraria();
        return Task.CompletedTask;
    }
}
