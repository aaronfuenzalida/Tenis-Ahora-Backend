using System.Text.Json.Serialization;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

public class Torneo : Competencia
{
    [JsonPropertyName("bracket")]
    public List<Ronda> Cuadro { get; set; } = [];

    public override List<Ronda> ObtenerRondas() => Cuadro ??= [];
}
