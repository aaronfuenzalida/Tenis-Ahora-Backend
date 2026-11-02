namespace TenisAhora.Application.Administracion.Interfaces;

// Datos iniciales (seed/seed.json): el mismo dataset que usa el Modo Demo del front.
public interface ICargadorSeed
{
    // Devuelve la cantidad de registros cargados; 0 si la base ya tenía datos.
    Task<int> CargarSiEstaVaciaAsync();

    // Vacía la base y vuelve a cargar el seed.
    Task<int> ResetearAsync();
}
