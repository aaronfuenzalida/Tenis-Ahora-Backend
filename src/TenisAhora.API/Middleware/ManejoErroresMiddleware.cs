using TenisAhora.Domain.Exceptions;

namespace TenisAhora.API.Middleware;

public class ManejoErroresMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ManejoErroresMiddleware> _logger;

    public ManejoErroresMiddleware(RequestDelegate next, ILogger<ManejoErroresMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (status, mensaje) = ex switch
            {
                EmailYaRegistradoException => (StatusCodes.Status409Conflict, ex.Message),
                CredencialesInvalidasException => (StatusCodes.Status401Unauthorized, ex.Message),
                _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Excepción no manejada en {Ruta}", context.Request.Path);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { error = mensaje });
        }
    }
}