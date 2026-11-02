using System.Text.Json.Serialization;

namespace TenisAhora.Domain.ValueObjects;

public class TituloHabilitante
{
    [JsonPropertyName("titleName")]
    public string Denominacion { get; set; } = string.Empty;

    [JsonPropertyName("institution")]
    public string Institucion { get; set; } = string.Empty;

    [JsonPropertyName("licenseNumber")]
    public string NumeroLicencia { get; set; } = string.Empty;

    [JsonPropertyName("issueDate")]
    public string FechaEmision { get; set; } = string.Empty;

    [JsonPropertyName("expiryDate")]
    public string Vencimiento { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("document")]
    public DocumentoRef? Documento { get; set; }
}
