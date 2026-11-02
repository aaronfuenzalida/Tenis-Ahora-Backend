using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Documentos.Dtos;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Documentos.Services;

public class DocumentoService : IDocumentoService
{
    private const int TamanioMaximo = 5 << 20;

    // El tipo sale del contenido, no de lo que declara el cliente.
    private static readonly (byte[] Firma, string TipoMime)[] TiposAceptados =
    [
        (new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, "application/pdf"),
        (new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png"),
    ];

    private readonly IRepositorio<Documento> _documentoRepository;

    public DocumentoService(IRepositorio<Documento> documentoRepository)
    {
        _documentoRepository = documentoRepository;
    }

    public async Task<DocumentoRef> SubirAsync(SubirDocumentoDto dto)
    {
        var nombre = (dto.Name ?? string.Empty).Trim();
        if (nombre == string.Empty)
            throw new DatosInvalidosException("El nombre del archivo es obligatorio.");

        byte[] contenido;
        try
        {
            contenido = Convert.FromBase64String(dto.Data ?? string.Empty);
        }
        catch (FormatException)
        {
            throw new DatosInvalidosException("El contenido del archivo no es base64 válido.");
        }
        if (contenido.Length == 0)
            throw new DatosInvalidosException("El contenido del archivo no es base64 válido.");
        if (contenido.Length > TamanioMaximo)
            throw new DatosInvalidosException("El archivo supera el máximo de 5 MB.");

        var tipo = TiposAceptados.FirstOrDefault(t => EmpiezaCon(contenido, t.Firma)).TipoMime
            ?? throw new DatosInvalidosException("Solo se aceptan archivos PDF, JPG o PNG.");

        var documento = new Documento
        {
            Id = await _documentoRepository.IdLibreAsync($"doc-{Formato.Milisegundos()}"),
            Nombre = nombre,
            TipoMime = tipo,
            Tamanio = contenido.Length,
            Contenido = contenido,
            SubidoEn = Formato.Iso()
        };
        await _documentoRepository.AgregarAsync(documento);
        return documento.Referencia();
    }

    public async Task<Documento> ObtenerAsync(string id) =>
        await _documentoRepository.ObtenerAsync(id) ?? throw new NoEncontradoException($"No existe documentos/{id}.");

    public async Task<DocumentoRef> ResolverReferenciaAsync(string? id)
    {
        if (string.IsNullOrEmpty(id))
            throw new DatosInvalidosException("Falta el documento adjunto.");
        var documento = await _documentoRepository.ObtenerAsync(id)
            ?? throw new DatosInvalidosException($"El documento {id} no existe.");
        return documento.Referencia();
    }

    private static bool EmpiezaCon(byte[] contenido, byte[] firma)
    {
        if (contenido.Length < firma.Length)
            return false;
        for (var i = 0; i < firma.Length; i++)
        {
            if (contenido[i] != firma[i])
                return false;
        }
        return true;
    }
}
