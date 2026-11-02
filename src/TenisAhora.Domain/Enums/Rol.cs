using System.Text.Json.Serialization;

namespace TenisAhora.Domain.Enums;

// En el JSON de la API el rol va como lo usa el front: client, admin o coach.
[JsonConverter(typeof(JsonStringEnumConverter<Rol>))]
public enum Rol
{
    [JsonStringEnumMemberName("client")]
    Socio = 1,

    [JsonStringEnumMemberName("admin")]
    Empleado = 2,

    [JsonStringEnumMemberName("coach")]
    Profesional = 3
}
