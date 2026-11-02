using System.Text.Json.Nodes;
using TenisAhora.Application.Caja.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Application.Pagos.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Caja.Services;

public class ReciboService : ServicioCrud<Recibo>, IReciboService
{
    private readonly IPagoService _pagoService;

    public ReciboService(IRepositorio<Recibo> repositorio, IPagoService pagoService) : base(repositorio)
    {
        _pagoService = pagoService;
    }

    protected override string Coleccion => "recibos";

    protected override object? Visible(UsuarioActual usuario, Recibo recibo) =>
        usuario.EsPersonal || (usuario.EsSocio && usuario.EsSuDni(recibo.DniCliente)) ? recibo : null;

    // Un socio o un profesional registra desde el portal los recibos de sus propios pagos
    // (inscripciones, cuotas, turnos).
    protected override void AutorizarAlta(UsuarioActual usuario, JsonObject cuerpo)
    {
        if (usuario.EsPersonal)
            return;
        if (!usuario.EsSocio && !usuario.EsProfesional)
            throw AccesoDenegadoException.SoloPersonal();
        if (!usuario.EsSuDni(Json.Texto(cuerpo, "clientDni")))
            throw new AccesoDenegadoException("Solo podés registrar pagos a tu nombre.");
    }

    // El front propone el número del recibo; si ya está tomado se genera otro.
    protected override async Task<string> ResolverIdAsync(string idPedido) =>
        string.IsNullOrEmpty(idPedido) || await _repositorio.ExisteAsync(idPedido) ? await NuevoIdAsync() : idPedido;

    protected override Task<string> NuevoIdAsync() => GeneradorIds.NuevoIdReciboAsync(_repositorio);

    // Todo recibo corresponde a un cobro electrónico aprobado (RD11).
    protected override async Task PrepararAltaAsync(Recibo recibo, JsonObject cuerpo, UsuarioActual usuario)
    {
        recibo.Fecha = Formato.SiVacio(recibo.Fecha, Formato.MarcaHoraria());
        recibo.Estado = Formato.SiVacio(recibo.Estado, "completado");
        recibo.Cajero = Formato.SiVacio(recibo.Cajero, "Administración General");
        if (recibo.TotalPagado <= 0)
            throw new DatosInvalidosException("El importe del recibo debe ser mayor a cero.");

        var pago = await _pagoService.ConsumirAsync(usuario, recibo.TransaccionId, recibo.TotalPagado, $"recibos/{recibo.Id}");
        recibo.AplicarPago(pago);
    }
}
