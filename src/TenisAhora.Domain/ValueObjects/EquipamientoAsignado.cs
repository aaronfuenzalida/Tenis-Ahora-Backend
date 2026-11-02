using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

// Equipamiento obligatorio de cada alquiler: una red, un tubo de pelotas y una raqueta por jugador.
public class EquipamientoAsignado
{
    [JsonPropertyName("nets")]
    public int Redes { get; set; }

    [JsonPropertyName("ballTubes")]
    public int TubosPelotas { get; set; }

    [JsonPropertyName("rackets")]
    public int Raquetas { get; set; }

    public static EquipamientoAsignado Para(string modalidad) => new()
    {
        Redes = 1,
        TubosPelotas = 1,
        Raquetas = modalidad == "dobles" ? 4 : 2
    };

    // Cantidad que hace falta de una categoría del stock.
    public int CantidadDe(string categoria) => categoria switch
    {
        "Redes" => Redes,
        "Pelotas" => TubosPelotas,
        "Raquetas" => Raquetas,
        _ => 0
    };
}
