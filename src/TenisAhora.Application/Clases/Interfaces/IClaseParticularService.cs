using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;

namespace TenisAhora.Application.Clases.Interfaces;

public interface IClaseParticularService : IServicioCrud
{
    Task<ClaseParticularCanceladaDto> CancelarAsync(string id, UsuarioActual usuario);
}
