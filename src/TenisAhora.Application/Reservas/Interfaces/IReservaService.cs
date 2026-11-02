using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Reservas.Dtos;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Reservas.Interfaces;

public interface IReservaService
{
    Task<List<object>> ListarAsync(UsuarioActual usuario);
    Task<List<OcupacionDto>> ListarOcupacionAsync(UsuarioActual usuario);
    Task<object> ObtenerAsync(string id, UsuarioActual usuario);
    Task<ReservaCreadaDto> CrearAsync(CrearReservaDto dto, UsuarioActual usuario);
    Task<object> ActualizarAsync(string id, JsonObject cambios, UsuarioActual usuario);
    Task EliminarAsync(string id, UsuarioActual usuario);
    Task<CobroReservaDto> PagarSenaAsync(string id, string? transaccionId, UsuarioActual usuario);
    Task<Reserva> IniciarAsync(string id);
    Task<CobroReservaDto> PagarSaldoAsync(string id, string? transaccionId, UsuarioActual usuario);
    Task<CancelacionReservaDto> CancelarAsync(string id, UsuarioActual usuario);
}
