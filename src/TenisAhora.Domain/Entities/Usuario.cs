using System.Text.Json.Serialization;
using TenisAhora.Domain.Enums;

namespace TenisAhora.Domain.Entities;

public class Usuario : IEntidad
{
    // Contraseña de los usuarios del seed y de los que da de alta el personal sin indicar una.
    public const string ContraseniaPorDefecto = "123456";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // Va antes que nombre y apellido: al leer un JSON que trae los tres, los campos sueltos ganan.
    [JsonPropertyName("name")]
    public string NombreCompleto
    {
        get => $"{Nombre} {Apellido}".Trim();
        set
        {
            var partes = (value ?? string.Empty).Trim().Split(' ', 2);
            Nombre = partes[0];
            Apellido = partes.Length > 1 ? partes[1] : string.Empty;
        }
    }

    [JsonPropertyName("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [JsonPropertyName("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [JsonPropertyName("dni")]
    public string? Dni { get; set; }

    [JsonPropertyName("phone")]
    public string? NumeroTelefono { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Direccion { get; set; }

    [JsonIgnore]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("role")]
    public Rol Rol { get; set; } = Rol.Socio;

    // Ficha de profesional a la que está vinculado un usuario con rol coach.
    [JsonPropertyName("coachId")]
    public string? ProfesorId { get; set; }

    [JsonPropertyName("memberNumber")]
    public string NumeroSocio { get; set; } = string.Empty;

    [JsonPropertyName("memberSince")]
    public string SocioDesde { get; set; } = string.Empty;

    [JsonPropertyName("activeDiscounts")]
    public List<string> DescuentosActivos { get; set; } = [];

    [JsonPropertyName("totalBookings")]
    public int TotalReservas { get; set; }

    [JsonPropertyName("photo")]
    public string? Foto { get; set; }
}
