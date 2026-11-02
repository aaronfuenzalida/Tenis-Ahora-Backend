using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Caja.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/facturas")]
public class FacturasController : CrudController
{
    public FacturasController(IFacturaService servicio) : base(servicio) { }
}
