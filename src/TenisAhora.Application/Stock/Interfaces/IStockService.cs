using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Stock.Interfaces;

public interface IStockService : IServicioCrud
{
    Task<ItemStock> AjustarAsync(string id, int? total);
}
