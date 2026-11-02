using TenisAhora.Application.Competencias.Dtos;

namespace TenisAhora.Application.Competencias.Interfaces;

public interface ILigaService : IServicioCompetencia
{
    Task<ResultadoLigaDto> RegistrarResultadoAsync(string id, string enfrentamientoId, ResultadoDto dto);
}
