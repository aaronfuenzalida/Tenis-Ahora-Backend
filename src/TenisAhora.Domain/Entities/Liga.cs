using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Liga : Competencia
{
    [JsonPropertyName("standings")]
    public List<Posicion> Posiciones { get; set; } = [];

    [JsonPropertyName("fixtures")]
    public List<Ronda> Fixture { get; set; } = [];

    public override List<Ronda> ObtenerRondas() => Fixture ??= [];

    // Reasigna el puesto según el orden actual de la tabla.
    public static void Renumerar(List<Posicion> posiciones)
    {
        for (var i = 0; i < posiciones.Count; i++)
            posiciones[i].Puesto = i + 1;
    }
}
