using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Competencias.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/tipos-competencia")]
public class TiposCompetenciaController : CrudController
{
    public TiposCompetenciaController(ITipoCompetenciaService servicio) : base(servicio) { }
}
