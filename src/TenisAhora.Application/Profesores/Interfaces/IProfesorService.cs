using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Profesores.Interfaces;

public interface IProfesorService : IServicioCrud
{
    Task<Profesor> ActualizarDisponibilidadAsync(string id, string? horarios, UsuarioActual usuario);
    Task<Profesor> ActualizarCertificacionAsync(string id, JsonObject cambios);

    // Una clase o turno particular tiene que respetar el tipo del profesional: clase o entrenamiento.
    Task VerificarTipoDeClaseAsync(string? profesorId, string? tipoDeClase);

    // RD09: el profesional a cargo tiene que tener el título habilitante vigente.
    Task VerificarHabilitadoAsync(string? profesorId);
}
