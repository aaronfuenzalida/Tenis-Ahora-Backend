using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Competencias.Dtos;

namespace TenisAhora.Application.Competencias.Interfaces;

// Reglamento, inscripciones y enfrentamientos: lo que comparten torneos y ligas.
public interface IServicioCompetencia : IServicioCrud
{
    Task<object> ActualizarReglamentoAsync(string id, JsonObject cuerpo);
    Task<InscripcionRealizadaDto> InscribirAsync(string id, InscripcionDto dto, UsuarioActual usuario);
    Task DesinscribirAsync(string id, string participanteId);
    Task<EnfrentamientoGuardadoDto> AgregarEnfrentamientoAsync(string id, NuevoEnfrentamientoDto dto);
    Task<EnfrentamientoGuardadoDto> ActualizarEnfrentamientoAsync(string id, string enfrentamientoId, JsonObject cambios);
}
