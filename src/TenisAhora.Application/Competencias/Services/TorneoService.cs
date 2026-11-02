using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Competencias.Interfaces;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Application.Competencias.Services;

public class TorneoService : ServicioCompetencia<Torneo>, ITorneoService
{
    public TorneoService(IRepositorio<Torneo> repositorio, IDocumentoService documentoService) : base(repositorio, documentoService) { }

    protected override string Coleccion => "torneos";
    protected override string PrefijoId => "trn";
    protected override string Descripcion => "este torneo";
    protected override string PrefijoParticipante => "p";
    protected override string PrefijoEnfrentamiento => "tm";
    protected override string RondaPorDefecto => "Ronda";
    protected override string ReglamentoPorDefecto => "Reglamento Interno: Partidos al mejor de 3 sets con tie-break oficial.";
}
