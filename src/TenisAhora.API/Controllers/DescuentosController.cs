using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Caja.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/descuentos")]
public class DescuentosController : CrudController
{
    public DescuentosController(IDescuentoService servicio) : base(servicio) { }
}
