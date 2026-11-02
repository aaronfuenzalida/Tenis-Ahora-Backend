using System.Text.Json.Nodes;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Application.Competencias.Dtos;
using TenisAhora.Application.Competencias.Interfaces;
using TenisAhora.Application.Documentos.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Application.Competencias.Services;

public class LigaService : ServicioCompetencia<Liga>, ILigaService
{
    public LigaService(IRepositorio<Liga> repositorio, IDocumentoService documentoService) : base(repositorio, documentoService) { }

    protected override string Coleccion => "ligas";
    protected override string PrefijoId => "liga";
    protected override string Descripcion => "esta liga";
    protected override string PrefijoParticipante => "lp";
    protected override string PrefijoEnfrentamiento => "lm";
    protected override string RondaPorDefecto => "Jornada";
    protected override string ReglamentoPorDefecto => "Reglamento Interno: Sistema todos contra todos por puntos.";

    // Cada inscripto tiene su fila en la tabla.
    protected override void AlInscribir(Liga liga, ParticipanteCompetencia participante)
    {
        if (liga.Posiciones.All(p => p.Nombre != participante.Nombre))
            liga.Posiciones.Add(new Posicion { Puesto = liga.Posiciones.Count + 1, Nombre = participante.Nombre });
    }

    protected override void AlDesinscribir(Liga liga, ParticipanteCompetencia? quitado)
    {
        if (quitado != null)
            liga.Posiciones = liga.Posiciones.Where(p => p.Nombre != quitado.Nombre).ToList();
        Liga.Renumerar(liga.Posiciones);
    }

    // Marca el partido como finalizado y actualiza la tabla: 3 puntos por victoria.
    public async Task<ResultadoLigaDto> RegistrarResultadoAsync(string id, string enfrentamientoId, ResultadoDto dto)
    {
        var liga = await ObtenerExistenteAsync(id);
        var ganador = dto.Winner ?? string.Empty;
        if (ganador == string.Empty)
            throw new DatosInvalidosException("Debe indicar el ganador.");

        var anterior = liga.ObtenerRondas().SelectMany(r => r.Enfrentamientos).FirstOrDefault(e => e.Id == enfrentamientoId)
            ?? throw new NoEncontradoException($"No existe el enfrentamiento {enfrentamientoId}.");
        if (ganador != anterior.Jugador1 && ganador != anterior.Jugador2)
            throw new DatosInvalidosException("El ganador debe ser uno de los dos jugadores del partido.");

        var yaFinalizado = anterior.Estado == "finalizado";
        var partido = ActualizarEnfrentamiento(liga.ObtenerRondas(), enfrentamientoId, new JsonObject
        {
            ["score"] = dto.Score ?? string.Empty,
            ["winner"] = ganador,
            ["status"] = "finalizado"
        });

        // Un resultado que se vuelve a cargar no suma otra vez a la tabla.
        if (!yaFinalizado)
        {
            var perdedor = ganador == partido.Jugador1 ? partido.Jugador2 : partido.Jugador1;
            foreach (var fila in liga.Posiciones)
            {
                if (fila.Nombre == ganador)
                {
                    fila.Jugados++;
                    fila.Ganados++;
                    fila.Puntos += 3;
                    fila.SetsAFavor += 2;
                    fila.Diferencia += 2;
                }
                else if (fila.Nombre == perdedor)
                {
                    fila.Jugados++;
                    fila.Perdidos++;
                    fila.SetsEnContra += 2;
                    fila.Diferencia -= 2;
                }
            }
            liga.Posiciones = liga.Posiciones.OrderByDescending(f => f.Puntos).ThenByDescending(f => f.Diferencia).ToList();
            Liga.Renumerar(liga.Posiciones);
        }

        await _repositorio.ActualizarAsync(liga);
        return new ResultadoLigaDto(true, partido, liga.Posiciones);
    }
}
