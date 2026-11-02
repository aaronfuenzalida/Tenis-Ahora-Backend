using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TenisAhora.Application.Common;
using TenisAhora.Application.Documentos.Dtos;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.API.Controllers;

// Reglamentos internos y títulos habilitantes: los sube el personal y los descarga cualquier usuario logueado.
[ApiController]
[Route("api/documentos")]
[Authorize]
public class DocumentosController : ControllerBase
{
    private readonly IDocumentoService _documentoService;

    public DocumentosController(IDocumentoService documentoService)
    {
        _documentoService = documentoService;
    }

    [HttpPost]
    [Authorize(Roles = UsuarioActual.RolEmpleado)]
    public async Task<ActionResult<DocumentoRef>> Subir(SubirDocumentoDto dto)
    {
        return StatusCode(StatusCodes.Status201Created, await _documentoService.SubirAsync(dto));
    }

    // Devuelve el archivo tal cual, no JSON.
    [HttpGet("{id}")]
    public async Task<IActionResult> Descargar(string id)
    {
        var documento = await _documentoService.ObtenerAsync(id);
        var disposicion = new ContentDispositionHeaderValue("inline");
        disposicion.SetHttpFileName(documento.Nombre);
        Response.Headers.ContentDisposition = disposicion.ToString();
        return File(documento.Contenido, documento.TipoMime);
    }
}
