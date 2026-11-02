using System.Text.Json.Nodes;
using TenisAhora.Application.Canchas.Dtos;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Canchas.Interfaces;

public interface ICanchaService : IServicioCrud
{
    Task<Cancha> CambiarEstadoAsync(string id, EstadoCanchaDto dto);
    Task<Cancha> ActualizarDisponibilidadAsync(string id, JsonObject cambios);
}
