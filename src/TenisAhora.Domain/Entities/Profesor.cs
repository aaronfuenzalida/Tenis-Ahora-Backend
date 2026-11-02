using System.Text.Json.Serialization;
using TenisAhora.Domain.Exceptions;
using TenisAhora.Domain.ValueObjects;

namespace TenisAhora.Domain.Entities;

// Ficha de un profesional: un Profesor da clases y un Entrenador da entrenamientos.
public class Profesor : IEntidad
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Tipo { get; set; } = string.Empty;

    [JsonPropertyName("dni")]
    public string? Dni { get; set; }

    [JsonPropertyName("phone")]
    public string? Telefono { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("hasOfficialLicense")]
    public bool TieneLicenciaOficial { get; set; }

    [JsonPropertyName("availableHours")]
    public string? HorariosDisponibles { get; set; }

    [JsonPropertyName("licenseDetails")]
    public TituloHabilitante? Titulo { get; set; }

    [JsonPropertyName("photo")]
    public string? Foto { get; set; }

    // Valor del campo kind de las clases que puede dar cada tipo de profesional.
    public static string TipoDeClaseSegun(string? tipo) => tipo == "Entrenador" ? "entrenamiento" : "clase";

    // RD09: sin título habilitante vigente no puede tener clases, entrenamientos ni turnos particulares.
    public void VerificarHabilitado(string hoy)
    {
        var titulo = Titulo ?? new TituloHabilitante();
        if (string.IsNullOrEmpty(titulo.Documento?.Id))
            throw new DatosInvalidosException($"{Nombre} no tiene cargado el documento del título habilitante (RD08).");
        if (titulo.Estado != "habilitado")
            throw new DatosInvalidosException($"El título habilitante de {Nombre} no está habilitado (RD09).");
        if (string.CompareOrdinal(titulo.Vencimiento, hoy) < 0)
            throw new DatosInvalidosException($"El título habilitante de {Nombre} venció el {titulo.Vencimiento} (RD09).");
    }
}
