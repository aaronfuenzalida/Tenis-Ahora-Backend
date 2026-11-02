using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Entities;

public class ItemStock : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Categoria { get; set; } = string.Empty;

    [JsonPropertyName("totalStock")]
    public int Total { get; set; }

    // Lo que está prestado en canchas con un alquiler en juego.
    [JsonPropertyName("inUseStock")]
    public int EnUso { get; set; }

    [JsonPropertyName("availableStock")]
    public int Disponible { get; set; }

    [JsonPropertyName("unit")]
    public string Unidad { get; set; } = string.Empty;

    [JsonPropertyName("itemPrice")]
    public decimal Precio { get; set; }

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Descripcion { get; set; } = string.Empty;

    // El ítem queda agotado (no se puede reservar) cuando no queda disponible.
    public void FijarEnUso(int enUso)
    {
        EnUso = enUso;
        Disponible = Math.Max(Total - enUso, 0);
        Estado = Disponible <= 0 ? "agotado" : "disponible";
    }
}
