using TenisAhora.Application.Clases.Dtos;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;

namespace TenisAhora.Application.Clases.Interfaces;

public interface IClaseService : IServicioCrud
{
    Task<AlumnoInscriptoDto> InscribirAlumnoAsync(string id, AlumnoDto dto, UsuarioActual usuario);
    Task QuitarAlumnoAsync(string id, string alumnoId, UsuarioActual usuario);
    Task<AsistenciaActualizadaDto> AlternarAsistenciaAsync(string id, AsistenciaDto dto, UsuarioActual usuario);
    Task<AsistenciaActualizadaDto> MarcarSesionAsync(string id, AsistenciaDto dto, UsuarioActual usuario);
    Task RegistrarAsistenciaAsync(string id, AsistenciaDto dto, UsuarioActual usuario);
}
