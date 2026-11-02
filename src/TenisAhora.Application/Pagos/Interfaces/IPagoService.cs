using TenisAhora.Application.Common;
using TenisAhora.Application.Pagos.Dtos;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Pagos.Interfaces;

public interface IPagoService
{
    Task<Pago> CrearAsync(CrearPagoDto dto, UsuarioActual usuario);

    // Valida que el pago exista, esté aprobado, sea de quien lo usa (el personal puede usar cualquiera),
    // no se haya usado y cubra el importe; lo marca como usado por referencia. Va después de las demás
    // validaciones, justo antes de guardar.
    Task<Pago> ConsumirAsync(UsuarioActual usuario, string? transaccionId, decimal monto, string referencia);
}
