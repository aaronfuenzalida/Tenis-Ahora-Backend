using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using TenisAhora.API.Extensions;
using TenisAhora.API.Middleware;
using TenisAhora.Application.Administracion.Interfaces;
using TenisAhora.Application.Common.Interfaces;
using TenisAhora.Domain.Entities;
using TenisAhora.Infrastructure.Persistence;

// El deploy monta la configuración en /app/.env; se carga antes de que el builder lea el entorno.
CargarDotEnv(".env");

// Los montos y las horas van en los mensajes con punto decimal, sin importar el idioma del servidor.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (port is not null)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var rutaBase = LeerVariable(builder.Configuration, "DATABASE_PATH") ?? "data/tenis_ahora.sqlite";
var rutaSeed = LeerVariable(builder.Configuration, "SEED_PATH") ?? "seed/seed.json";
var carpetaFront = LeerVariable(builder.Configuration, "FRONTEND_DIST") ?? "../Tenis-Ahora-Frontend/dist";
var jwtSecret = LeerVariable(builder.Configuration, "JWT_SECRET");
if (jwtSecret is not null)
    builder.Configuration["Jwt:Key"] = jwtSecret;

var carpetaBase = Path.GetDirectoryName(Path.GetFullPath(rutaBase));
if (!string.IsNullOrEmpty(carpetaBase))
    Directory.CreateDirectory(carpetaBase);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Un cuerpo que no se puede leer responde con el mismo formato de error que el resto: { error }.
        options.InvalidModelStateResponseFactory = context =>
        {
            var detalle = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrEmpty(m));
            return new BadRequestObjectResult(new { error = $"Cuerpo JSON inválido: {detalle}" });
        };
    });
builder.Services.AddOpenApi();

// El front se sirve desde otro dominio (Firebase): se acepta cualquier origen, método y header.
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddHttpLogging(options =>
    options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath
        | HttpLoggingFields.ResponseStatusCode | HttpLoggingFields.Duration);

builder.Services.AddDbContext<TenisAhoraDbContext>(options =>
    options.UseSqlite($"Data Source={rutaBase}"));
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddUsuariosModule();
builder.Services.AddCanchasModule();
builder.Services.AddStockModule();
builder.Services.AddPagosModule();
builder.Services.AddReservasModule();
builder.Services.AddCompetenciasModule();
builder.Services.AddProfesoresModule();
builder.Services.AddClasesModule();
builder.Services.AddCajaModule();
builder.Services.AddDocumentosModule();
builder.Services.AddAdministracionModule(rutaSeed);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Los claims quedan con el nombre que tienen en el token: sub, role, dni, coachId.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };
        // 401 y 403 con el mismo formato de error que el resto de la API.
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                var mensaje = context.AuthenticateFailure switch
                {
                    null => "Falta el token de autenticación (Authorization: Bearer ...).",
                    SecurityTokenExpiredException => "Token rechazado: Token vencido",
                    _ => "Token rechazado: firma o formato inválido"
                };
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = mensaje });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Esta acción es solo para el personal del club." });
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseHttpLogging();
app.UseMiddleware<ManejoErroresMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();

// Si está el build del front, la API lo sirve en / (las rutas de react-router devuelven index.html).
var indexFront = Path.GetFullPath(Path.Combine(carpetaFront, "index.html"));
if (File.Exists(indexFront))
{
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.GetFullPath(carpetaFront)) });
    app.Logger.LogInformation("Sirviendo el front desde {Carpeta}", carpetaFront);
}
else
{
    app.Logger.LogInformation("Sin build del front en {Carpeta} (solo API). Generarlo con `npm run build` en el front.", carpetaFront);
}

app.UseAuthentication();   // ¿quien sos? (lee y valida el token)
app.UseAuthorization();    // ¿podés hacer esto? (evalua [Authorize])

app.MapControllers();

if (File.Exists(indexFront))
{
    // Solo GET: un método no soportado en una ruta de la API sigue respondiendo 405.
    app.MapFallback("{*ruta:nonfile}", async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "La ruta no existe." });
            return;
        }
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexFront);
    }).WithMetadata(new HttpMethodMetadata(["GET", "HEAD"]));
}

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TenisAhoraDbContext>().Database.Migrate();
    try
    {
        var cargados = await scope.ServiceProvider.GetRequiredService<ICargadorSeed>().CargarSiEstaVaciaAsync();
        if (cargados > 0)
            app.Logger.LogInformation("Base vacía: se cargaron {Cantidad} registros desde {Seed}. Usuarios con contraseña {Contrasenia}.", cargados, rutaSeed, Usuario.ContraseniaPorDefecto);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "No se pudo cargar el seed {Seed}.", rutaSeed);
    }
}

app.Run();

// Una variable vacía cuenta como no definida (el .env del deploy puede traer JWT_SECRET=).
static string? LeerVariable(IConfiguration configuracion, string clave) =>
    string.IsNullOrWhiteSpace(configuracion[clave]) ? null : configuracion[clave];

// Carga .env si existe, sin pisar variables ya definidas en el entorno.
static void CargarDotEnv(string archivo)
{
    if (!File.Exists(archivo))
        return;
    foreach (var linea in File.ReadAllLines(archivo).Select(l => l.Trim()))
    {
        if (linea == string.Empty || linea.StartsWith('#'))
            continue;
        var partes = (linea.StartsWith("export ") ? linea["export ".Length..] : linea).Split('=', 2);
        if (partes.Length != 2)
            continue;
        var clave = partes[0].Trim();
        var valor = partes[1].Trim();
        if (valor.Length >= 2 && (valor[0] == '"' || valor[0] == '\'') && valor[^1] == valor[0])
            valor = valor[1..^1];
        if (Environment.GetEnvironmentVariable(clave) is null)
            Environment.SetEnvironmentVariable(clave, valor);
    }
}
