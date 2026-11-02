using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Caja.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/recibos")]
public class RecibosController : CrudController
{
    public RecibosController(IReciboService servicio) : base(servicio) { }
}
