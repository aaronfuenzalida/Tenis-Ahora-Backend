using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Stock.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Stock.Services;

public class StockService : ServicioCrud<ItemStock>, IStockService
{
    public StockService(IRepositorio<ItemStock> repositorio) : base(repositorio) { }

    protected override string Coleccion => "stock";

    protected override Task PrepararAltaAsync(ItemStock item, JsonObject cuerpo, UsuarioActual usuario)
    {
        item.Categoria = Formato.SiVacio(item.Categoria, "Equipamiento");
        item.Descripcion ??= string.Empty;
        item.Unidad = Formato.SiVacio(item.Unidad, "unidades");
        item.FijarEnUso(item.EnUso);
        return Task.CompletedTask;
    }

    protected override Task PrepararCambioAsync(ItemStock actual, ItemStock nuevo, JsonObject cambios, UsuarioActual usuario)
    {
        VerificarTotal(nuevo.Total, nuevo.EnUso);
        nuevo.FijarEnUso(nuevo.EnUso);
        return Task.CompletedTask;
    }

    public async Task<ItemStock> AjustarAsync(string id, int? total)
    {
        var item = await ObtenerExistenteAsync(id);
        var nuevoTotal = Math.Max(total ?? 0, 0);
        VerificarTotal(nuevoTotal, item.EnUso);

        item.Total = nuevoTotal;
        item.FijarEnUso(item.EnUso);
        await _repositorio.ActualizarAsync(item);
        return item;
    }

    private static void VerificarTotal(int total, int enUso)
    {
        if (total < enUso)
            throw new DatosInvalidosException($"El stock total no puede ser menor a lo que está prestado en canchas ({enUso}).");
    }
}
