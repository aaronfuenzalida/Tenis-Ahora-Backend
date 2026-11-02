using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Canchas.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/tipos-cancha")]
public class TiposCanchaController : CrudController
{
    public TiposCanchaController(ITipoCanchaService servicio) : base(servicio) { }
}
