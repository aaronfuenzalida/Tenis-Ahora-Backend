using System.Text.Json.Nodes;
using TenisAhora.Application.Canchas.Dtos;
using TenisAhora.Application.Canchas.Interfaces;
using TenisAhora.Application.Common;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Common.Services;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Canchas.Services;

public class CanchaService : ServicioCrud<Cancha>, ICanchaService
{
    private const string FotoPorDefecto = "https://images.unsplash.com/photo-1595435934249-5df7ed86e1c0?auto=format&fit=crop&w=600&q=80";
    private static readonly string[] Estados = ["disponible", "ocupada", "mantenimiento"];

    public CanchaService(IRepositorio<Cancha> repositorio) : base(repositorio) { }

    protected override string Coleccion => "canchas";
    protected override string PrefijoId => "cancha";

    protected override Task PrepararAltaAsync(Cancha cancha, JsonObject cuerpo, UsuarioActual usuario)
    {
        cancha.Estado = Formato.SiVacio(cancha.Estado, "disponible");
        cancha.NotasMantenimiento ??= string.Empty;
        cancha.Disponibilidad ??= new Disponibilidad();
        cancha.Foto = Formato.SiVacio(cancha.Foto, FotoPorDefecto);
        return Task.CompletedTask;
    }

    public async Task<Cancha> CambiarEstadoAsync(string id, EstadoCanchaDto dto)
    {
        var cancha = await ObtenerExistenteAsync(id);
        var estado = dto.Status ?? string.Empty;
        if (!Estados.Contains(estado))
            throw new DatosInvalidosException("status debe ser disponible, ocupada o mantenimiento.");

        cancha.Estado = estado;
        cancha.NotasMantenimiento = dto.MaintenanceNotes ?? string.Empty;
        await _repositorio.ActualizarAsync(cancha);
        return cancha;
    }

    // Lo que llega se mezcla sobre la disponibilidad actual (los campos que faltan quedan como estaban).
    public async Task<Cancha> ActualizarDisponibilidadAsync(string id, JsonObject cambios)
    {
        var cancha = await ObtenerExistenteAsync(id);
        var disponibilidad = Json.Mezclar(cancha.Disponibilidad ?? new Disponibilidad(), cambios);
        if (disponibilidad.Dias is null || disponibilidad.Dias.Count == 0)
            throw new DatosInvalidosException("Debe indicar al menos un día de atención.");
        if (string.CompareOrdinal(disponibilidad.Cierre, disponibilidad.Apertura) <= 0)
            throw new DatosInvalidosException("El horario de cierre debe ser posterior al de apertura.");

        cancha.Disponibilidad = disponibilidad;
        await _repositorio.ActualizarAsync(cancha);
        return cancha;
    }
}
