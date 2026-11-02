using System.Text.Json.Nodes;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Competencias.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Competencias.Services;

public class TipoCompetenciaService : ServicioCrud<TipoCompetencia>, ITipoCompetenciaService
{
    public TipoCompetenciaService(IRepositorio<TipoCompetencia> repositorio) : base(repositorio) { }

    protected override string Coleccion => "tipos-competencia";
    protected override string PrefijoId => "cmp";

    protected override Task PrepararAltaAsync(TipoCompetencia tipo, JsonObject cuerpo, UsuarioActual usuario)
    {
        if (string.IsNullOrWhiteSpace(tipo.Nombre))
            throw new DatosInvalidosException("El nombre del tipo de competencia es obligatorio.");

        tipo.Formato ??= string.Empty;
        tipo.AplicaA = Formato.SiVacio(tipo.AplicaA, "Torneo");
        tipo.SistemaPuntuacion ??= string.Empty;
        tipo.Descripcion ??= string.Empty;
        if (tipo.MinimoParticipantes <= 0)
            tipo.MinimoParticipantes = 2;
        if (tipo.MaximoParticipantes <= 0)
            tipo.MaximoParticipantes = 32;
        return Task.CompletedTask;
    }
}
