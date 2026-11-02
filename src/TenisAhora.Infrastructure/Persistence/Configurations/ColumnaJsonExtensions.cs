using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace TenisAhora.Infrastructure.Persistence.Configurations;

public static class ColumnaJsonExtensions
{
    // Guarda el valor como JSON en una columna de texto. Es para lo que vive y cambia con su registro
    // (participantes, cuadro, alumnos, ítems...); el comparador detecta los cambios hechos adentro de
    // las listas.
    public static PropertyBuilder<T> ComoJson<T>(this PropertyBuilder<T> propiedad)
    {
        var conversor = new ValueConverter<T, string>(
            valor => JsonSerializer.Serialize(valor, JsonSerializerOptions.Web),
            json => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Web)!);

        var comparador = new ValueComparer<T>(
            (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Web) == JsonSerializer.Serialize(b, JsonSerializerOptions.Web),
            valor => JsonSerializer.Serialize(valor, JsonSerializerOptions.Web).GetHashCode(),
            valor => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(valor, JsonSerializerOptions.Web), JsonSerializerOptions.Web)!);

        return propiedad.HasConversion(conversor, comparador);
    }
}
