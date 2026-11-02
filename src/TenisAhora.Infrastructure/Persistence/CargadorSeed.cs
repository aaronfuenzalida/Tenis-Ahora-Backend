using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TenisAhora.Application.Administracion.Interfaces;
using TenisAhora.Application.Auth.Interfaces;
using TenisAhora.Domain.Entities;

namespace TenisAhora.Infrastructure.Persistence;

// El seed tiene la forma { coleccion: [registros] }, con los mismos campos que maneja el front.
public class CargadorSeed : ICargadorSeed
{
    private readonly TenisAhoraDbContext _context;
    private readonly IEstadoBase _estadoBase;
    private readonly IPasswordHasher _passwordHasher;
    private readonly SeedSettings _settings;

    public CargadorSeed(TenisAhoraDbContext context, IEstadoBase estadoBase, IPasswordHasher passwordHasher, IOptions<SeedSettings> options)
    {
        _context = context;
        _estadoBase = estadoBase;
        _passwordHasher = passwordHasher;
        _settings = options.Value;
    }

    public async Task<int> CargarSiEstaVaciaAsync()
    {
        var cantidades = await _estadoBase.ContarPorColeccionAsync();
        if (cantidades.Values.Sum() > 0)
            return 0;
        return await InsertarAsync(await LeerSeedAsync());
    }

    public async Task<int> ResetearAsync()
    {
        // Se lee antes de vaciar: si el archivo falla, la base queda como estaba.
        var seed = await LeerSeedAsync();

        await _context.Usuarios.ExecuteDeleteAsync();
        await _context.Canchas.ExecuteDeleteAsync();
        await _context.TiposCancha.ExecuteDeleteAsync();
        await _context.Stock.ExecuteDeleteAsync();
        await _context.Reservas.ExecuteDeleteAsync();
        await _context.Pagos.ExecuteDeleteAsync();
        await _context.Torneos.ExecuteDeleteAsync();
        await _context.Ligas.ExecuteDeleteAsync();
        await _context.TiposCompetencia.ExecuteDeleteAsync();
        await _context.Profesores.ExecuteDeleteAsync();
        await _context.Clases.ExecuteDeleteAsync();
        await _context.ClasesParticulares.ExecuteDeleteAsync();
        await _context.Recibos.ExecuteDeleteAsync();
        await _context.Facturas.ExecuteDeleteAsync();
        await _context.Reembolsos.ExecuteDeleteAsync();
        await _context.Descuentos.ExecuteDeleteAsync();
        await _context.Documentos.ExecuteDeleteAsync();
        _context.ChangeTracker.Clear();

        return await InsertarAsync(seed);
    }

    private async Task<JsonObject> LeerSeedAsync()
    {
        var contenido = await File.ReadAllTextAsync(_settings.Ruta);
        return JsonNode.Parse(contenido) as JsonObject
            ?? throw new InvalidOperationException($"El seed {_settings.Ruta} debe ser un objeto {{ coleccion: [...] }}.");
    }

    private async Task<int> InsertarAsync(JsonObject seed)
    {
        // Todos los usuarios del seed entran con la contraseña por defecto.
        var hash = _passwordHasher.Hash(Usuario.ContraseniaPorDefecto);
        var usuarios = Leer<Usuario>(seed, "usuarios");
        foreach (var usuario in usuarios)
            usuario.PasswordHash = hash;

        _context.Usuarios.AddRange(usuarios);
        _context.Canchas.AddRange(Leer<Cancha>(seed, "canchas"));
        _context.TiposCancha.AddRange(Leer<TipoCancha>(seed, "tipos-cancha"));
        _context.Stock.AddRange(Leer<ItemStock>(seed, "stock"));
        _context.Reservas.AddRange(Leer<Reserva>(seed, "reservas"));
        _context.Torneos.AddRange(Leer<Torneo>(seed, "torneos"));
        _context.Ligas.AddRange(Leer<Liga>(seed, "ligas"));
        _context.TiposCompetencia.AddRange(Leer<TipoCompetencia>(seed, "tipos-competencia"));
        _context.Profesores.AddRange(Leer<Profesor>(seed, "profesores"));
        _context.Clases.AddRange(Leer<Clase>(seed, "clases"));
        _context.ClasesParticulares.AddRange(Leer<ClaseParticular>(seed, "clases-particulares"));
        _context.Recibos.AddRange(Leer<Recibo>(seed, "recibos"));
        _context.Facturas.AddRange(Leer<Factura>(seed, "facturas"));
        _context.Reembolsos.AddRange(Leer<Reembolso>(seed, "reembolsos"));
        _context.Descuentos.AddRange(Leer<Descuento>(seed, "descuentos"));
        _context.Documentos.AddRange(Leer<Documento>(seed, "documentos"));
        return await _context.SaveChangesAsync();
    }

    private static List<T> Leer<T>(JsonObject seed, string coleccion) =>
        seed[coleccion]?.Deserialize<List<T>>(JsonSerializerOptions.Web) ?? [];
}
