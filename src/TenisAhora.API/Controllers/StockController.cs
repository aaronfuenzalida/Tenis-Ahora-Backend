using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Common;
using TenisAhora.Application.Stock.Dtos;
using TenisAhora.Application.Stock.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.API.Controllers;

[Route("api/stock")]
public class StockController : CrudController
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService) : base(stockService)
    {
        _stockService = stockService;
    }

    [HttpPatch("{id}/ajuste")]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<ItemStock>> Ajustar(string id, AjusteStockDto dto)
    {
        return Ok(await _stockService.AjustarAsync(id, dto.TotalStock));
    }
}
