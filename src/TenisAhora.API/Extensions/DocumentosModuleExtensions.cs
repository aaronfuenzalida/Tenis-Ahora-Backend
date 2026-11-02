using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Application.Documentos.Services;

namespace TenisAhora.API.Extensions;

public static class DocumentosModuleExtensions
{
    public static IServiceCollection AddDocumentosModule(this IServiceCollection services)
    {
        services.AddScoped<IDocumentoService, DocumentoService>();

        return services;
    }
}
