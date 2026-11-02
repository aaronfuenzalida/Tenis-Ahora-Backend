using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// Fila de la tabla de una liga.
public class Posicion
{
    [JsonPropertyName("rank")]
    public int Puesto { get; set; }

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("pj")]
    public int Jugados { get; set; }

    [JsonPropertyName("pg")]
    public int Ganados { get; set; }

    [JsonPropertyName("pp")]
    public int Perdidos { get; set; }

    [JsonPropertyName("sf")]
    public int SetsAFavor { get; set; }

    [JsonPropertyName("sc")]
    public int SetsEnContra { get; set; }

    [JsonPropertyName("dif")]
    public int Diferencia { get; set; }

    [JsonPropertyName("points")]
    public int Puntos { get; set; }
}
