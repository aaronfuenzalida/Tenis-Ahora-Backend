using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Usuarios.Dtos;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Usuarios.Interfaces;

public interface IUsuarioService : IServicioCrud
{
    Task<Usuario> ActualizarDescuentosAsync(string id, List<string>? codigos);
    Task<List<SocioDto>> ListarSociosAsync();
}
