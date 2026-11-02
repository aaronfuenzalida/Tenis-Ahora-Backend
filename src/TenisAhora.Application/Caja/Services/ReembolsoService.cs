using System.Text.Json.Nodes;
using TenisAhora.Application.Caja.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Caja.Services;

public class ReembolsoService : ServicioCrud<Reembolso>, IReembolsoService
{
    public ReembolsoService(IRepositorio<Reembolso> repositorio) : base(repositorio) { }

    protected override string Coleccion => "reembolsos";

    protected override void AutorizarListado(UsuarioActual usuario) => usuario.ExigirPersonal();

    protected override object? Visible(UsuarioActual usuario, Reembolso reembolso) => usuario.EsPersonal ? reembolso : null;

    protected override Task<string> NuevoIdAsync() => GeneradorIds.NuevoIdReembolsoAsync(_repositorio);

    // Total si se devuelve todo lo abonado; parcial si se devuelve menos.
    protected override Task PrepararAltaAsync(Reembolso reembolso, JsonObject cuerpo, UsuarioActual usuario)
    {
        var monto = Math.Min(reembolso.MontoReembolsado, reembolso.MontoOriginal);
        if (monto <= 0)
            throw new DatosInvalidosException("El importe a reembolsar debe ser mayor a cero.");

        reembolso.MontoReembolsado = monto;
        reembolso.Tipo = monto >= reembolso.MontoOriginal ? "total" : "parcial";
        reembolso.MedioReembolso = Formato.SiVacio(reembolso.MedioReembolso, "Crédito en cuenta de socio");
        reembolso.ProcesadoPor = Formato.SiVacio(reembolso.ProcesadoPor, "Administración General");
        if (string.IsNullOrEmpty(reembolso.ReservaRelacionada))
            reembolso.ReservaRelacionada = null;
        if (string.IsNullOrEmpty(reembolso.ReciboRelacionado))
            reembolso.ReciboRelacionado = null;
        reembolso.Fecha = Formato.MarcaHoraria();
        return Task.CompletedTask;
    }
}
