using TenisAhora.Application.Stock.Interfaces;
using TenisAhora.Application.Stock.Services;

namespace TenisAhora.API.Extensions;

public static class StockModuleExtensions
{
    public static IServiceCollection AddStockModule(this IServiceCollection services)
    {
        services.AddScoped<IStockService, StockService>();

        return services;
    }
}
