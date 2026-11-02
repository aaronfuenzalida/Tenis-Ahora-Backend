using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Common.Interfaces;

public interface IRepositorio<T> where T : class, IEntidad
{
    Task<List<T>> ListarAsync();
    Task<T?> ObtenerAsync(string id);
    Task<bool> ExisteAsync(string id);
    Task<int> ContarAsync();
    Task AgregarAsync(T entidad);
    Task ActualizarAsync(T entidad);
    // Copia los valores de nuevo sobre el registro actual y guarda.
    Task ReemplazarAsync(T actual, T nuevo);
    Task EliminarAsync(T entidad);
}
