using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TenisAhora.API.Extensions;
using TenisAhora.Infrastructure.Persistence;
using TenisAhora.API.Middleware;

const string PoliticaCors = "FrontendLocal";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TenisAhoraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// El navegador bloquea las llamadas del front (otro puerto) si la API no declara el origen.
builder.Services.AddCors(options =>
{
    var origenesPermitidos = builder.Configuration
        .GetSection("Cors:OrigenesPermitidos")
        .Get<string[]>() ?? [];

    options.AddPolicy(PoliticaCors, policy => policy
        .WithOrigins(origenesPermitidos)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

builder.Services.AddAuthModule(builder.Configuration);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseMiddleware<ManejoErroresMiddleware>();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // En desarrollo el front habla por http://localhost:5090:
    // el redirect a https cortaria el preflight de CORS.
    app.UseHttpsRedirection();
}

app.UseCors(PoliticaCors);   // siempre antes de Authentication/Authorization

app.UseAuthentication();   // ¿quien sos? (lee y valida el token)
app.UseAuthorization();    // ¿podés hacer esto? (evalua [Authorize])

app.MapControllers();

app.Run();
