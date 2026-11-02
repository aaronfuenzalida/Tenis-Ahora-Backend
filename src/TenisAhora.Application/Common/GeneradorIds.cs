using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Common;

// Identificadores con los mismos prefijos que usa el front (usr-, cancha-, REC-2026-1050...).
public static class GeneradorIds
{
    // Si el id ya existe le agrega -2, -3...
    public static async Task<string> IdLibreAsync<T>(this IRepositorio<T> repositorio, string idBase) where T : class, IEntidad
    {
        var id = idBase;
        for (var n = 2; await repositorio.ExisteAsync(id); n++)
            id = $"{idBase}-{n}";
        return id;
    }

    public static async Task<string> NuevoIdReciboAsync(IRepositorio<Recibo> recibos)
    {
        var cantidad = await recibos.ContarAsync();
        return await recibos.IdLibreAsync($"REC-{DateTime.Now.Year}-{1050 + cantidad}");
    }

    public static async Task<string> NuevoIdReembolsoAsync(IRepositorio<Reembolso> reembolsos)
    {
        var cantidad = await reembolsos.ContarAsync();
        return await reembolsos.IdLibreAsync($"RMB-{DateTime.Now.Year}-{cantidad + 9:0000}");
    }
}
