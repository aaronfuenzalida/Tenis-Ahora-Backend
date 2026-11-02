using Microsoft.EntityFrameworkCore;
using TenisAhora.Application.Administracion.Interfaces;

namespace TenisAhora.Infrastructure.Persistence;

public class EstadoBase : IEstadoBase
{
    private readonly TenisAhoraDbContext _context;

    public EstadoBase(TenisAhoraDbContext context)
    {
        _context = context;
    }

    public async Task<string> VersionMotorAsync() =>
        await _context.Database.SqlQueryRaw<string>("SELECT sqlite_version() AS \"Value\"").SingleAsync();

    public async Task<Dictionary<string, int>> ContarPorColeccionAsync() => new()
    {
        ["canchas"] = await _context.Canchas.CountAsync(),
        ["clases"] = await _context.Clases.CountAsync(),
        ["clases-particulares"] = await _context.ClasesParticulares.CountAsync(),
        ["descuentos"] = await _context.Descuentos.CountAsync(),
        ["documentos"] = await _context.Documentos.CountAsync(),
        ["facturas"] = await _context.Facturas.CountAsync(),
        ["ligas"] = await _context.Ligas.CountAsync(),
        ["pagos"] = await _context.Pagos.CountAsync(),
        ["profesores"] = await _context.Profesores.CountAsync(),
        ["recibos"] = await _context.Recibos.CountAsync(),
        ["reembolsos"] = await _context.Reembolsos.CountAsync(),
        ["reservas"] = await _context.Reservas.CountAsync(),
        ["stock"] = await _context.Stock.CountAsync(),
        ["tipos-cancha"] = await _context.TiposCancha.CountAsync(),
        ["tipos-competencia"] = await _context.TiposCompetencia.CountAsync(),
        ["torneos"] = await _context.Torneos.CountAsync(),
        ["usuarios"] = await _context.Usuarios.CountAsync(),
    };
}
