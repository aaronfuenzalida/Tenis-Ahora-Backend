using System.Text.Json.Nodes;

namespace TenisAhora.Application.Common.Interfaces;

// Consulta, alta, modificación y baja de una colección. Devuelve object porque cada usuario puede
// ver una versión distinta del registro (por ejemplo, un socio ve las clases sin los alumnos).
public interface IServicioCrud
{
    Task<List<object>> ListarAsync(UsuarioActual usuario);
    Task<object> ObtenerAsync(string id, UsuarioActual usuario);
    Task<object> CrearAsync(JsonObject cuerpo, UsuarioActual usuario);
    Task<object> ActualizarAsync(string id, JsonObject cambios, UsuarioActual usuario);
    Task EliminarAsync(string id, UsuarioActual usuario);
}
