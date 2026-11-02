using TenisAhora.Application.Documentos.Dtos;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Documentos.Interfaces;

public interface IDocumentoService
{
    Task<DocumentoRef> SubirAsync(SubirDocumentoDto dto);
    Task<Documento> ObtenerAsync(string id);

    // Valida que la referencia apunte a un documento cargado y la devuelve completa (nombre, tipo, tamaño).
    Task<DocumentoRef> ResolverReferenciaAsync(string? id);
}
