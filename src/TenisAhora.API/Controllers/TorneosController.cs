using Microsoft.AspNetCore.Mvc;
using TenisAhora.Application.Competencias.Interfaces;

namespace TenisAhora.API.Controllers;

[Route("api/torneos")]
public class TorneosController : CompetenciasController
{
    public TorneosController(ITorneoService torneoService) : base(torneoService) { }
}
