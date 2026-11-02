using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Caja.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/reembolsos")]
public class ReembolsosController : CrudController
{
    public ReembolsosController(IReembolsoService servicio) : base(servicio) { }
}
