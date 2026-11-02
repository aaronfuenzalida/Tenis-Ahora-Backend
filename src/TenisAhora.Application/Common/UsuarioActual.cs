using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Common;

// Lo que trae el token: con esto se decide qué ve y qué modifica cada usuario sin releer su ficha.
//   - Empleado: todo.
//   - Socio: el catálogo del club, sus reservas y recibos; se inscribe y pide clases solo para sí mismo.
//   - Profesor / Entrenador: el catálogo, su ficha y las clases a su cargo.
public record UsuarioActual(string Id, string Email, string Rol, string Dni, string ProfesorId)
{
    public const string RolEmpleado = "Empleado";
    public const string RolSocio = "Socio";
    public const string RolProfesor = "Profesor";
    public const string RolEntrenador = "Entrenador";

    public bool EsPersonal => Rol == RolEmpleado;
    public bool EsSocio => Rol == RolSocio;
    public bool EsProfesional => Rol is RolProfesor or RolEntrenador;

    // Un usuario sin DNI cargado no es dueño de nada.
    public bool EsSuDni(string? dni) => Dni != string.Empty && dni == Dni;

    public bool Dicta(string? profesorId) => EsProfesional && profesorId == ProfesorId;

    public void ExigirPersonal()
    {
        if (!EsPersonal)
            throw AccesoDenegadoException.SoloPersonal();
    }

    public void ExigirSocioOPersonal()
    {
        if (!EsPersonal && !EsSocio)
            throw new AccesoDenegadoException("Esta sección es solo para socios.");
    }
}
