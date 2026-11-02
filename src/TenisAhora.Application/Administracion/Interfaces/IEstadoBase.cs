namespace TenisAhora.Application.Administracion.Interfaces;

public interface IEstadoBase
{
    Task<string> VersionMotorAsync();

    // Cantidad de registros por colección, con el nombre de la ruta.
    Task<Dictionary<string, int>> ContarPorColeccionAsync();
}
