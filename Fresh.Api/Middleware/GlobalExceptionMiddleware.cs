using System.Text.Json;

namespace Fresh.Api.Middleware;

/// <summary>
/// Capa más externa del pipeline. Responsabilidad única: capturar excepciones no
/// controladas y devolver una respuesta JSON estructurada (HTTP 500).
/// El logging queda delegado a ApiLoggingMiddleware (que está más adentro).
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Excepción no controlada en {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode  = 500;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            message   = ex.Message,
            traceId   = context.TraceIdentifier,
            timestamp = DateTimeOffset.UtcNow,
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
