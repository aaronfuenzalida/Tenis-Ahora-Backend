using System.Text.Json;
using System.Text.Json.Nodes;
using TenisAhora.Domain.Exceptions;

namespace TenisAhora.Application.Common;

// Los registros viajan con los nombres de campo del front (los [JsonPropertyName] de las entidades).
public static class Json
{
    public static T Convertir<T>(JsonNode cuerpo)
    {
        try
        {
            return cuerpo.Deserialize<T>(JsonSerializerOptions.Web)
                ?? throw new DatosInvalidosException("El cuerpo debe ser un objeto JSON.");
        }
        catch (JsonException ex)
        {
            throw new DatosInvalidosException("Cuerpo JSON inválido: " + ex.Message);
        }
    }

    // { ...actual, ...cambios }: las claves que llegan pisan las del registro (mezcla superficial).
    public static T Mezclar<T>(T actual, JsonObject cambios)
    {
        var mezcla = JsonSerializer.SerializeToNode(actual, JsonSerializerOptions.Web)!.AsObject();
        foreach (var (clave, valor) in cambios)
            mezcla[clave] = valor?.DeepClone();
        return Convertir<T>(mezcla);
    }

    // El texto de una clave, o "" si no existe o no es un string.
    public static string Texto(JsonNode? nodo, string clave) =>
        nodo is JsonObject objeto && objeto[clave] is JsonValue valor && valor.TryGetValue<string>(out var texto)
            ? texto
            : string.Empty;
}
