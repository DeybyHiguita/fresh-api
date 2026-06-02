using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Fresh.Core.DTOs.Log;
using Fresh.Core.Interfaces;

namespace Fresh.Api.Middleware;

/// <summary>
/// Responsabilidad única: registrar en la BD cada petición HTTP con su duración,
/// entidad afectada, usuario y excepción (si ocurrió).
/// Depende de ILogService (abstracción), no de FreshDbContext.
/// </summary>
public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    private static readonly string[] ExcludedPrefixes =
    [
        "/api/logs",
        "/swagger",
        "/favicon.ico",
    ];

    public ApiLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<ApiLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        var stopwatch = Stopwatch.StartNew();
        Exception? capturedException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            capturedException = ex;
            throw; // GlobalExceptionMiddleware formatea la respuesta 500
        }
        finally
        {
            stopwatch.Stop();

            // Extraer todos los datos del contexto ANTES de entrar al Task.Run
            var method      = context.Request.Method;
            var path        = context.Request.Path.Value ?? string.Empty;
            var statusCode  = capturedException is not null ? 500 : context.Response.StatusCode;
            var userId      = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var entityName  = ExtractEntityName(path);
            var entityId    = ExtractEntityId(path);
            var durationMs  = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);
            var exText      = capturedException?.ToString();

            _ = Task.Run(async () =>
                await PersistLogAsync(method, path, statusCode, userId,
                                      entityName, entityId, durationMs, exText));
        }
    }

    // ── Persistencia ──────────────────────────────────────────────────────────

    private async Task PersistLogAsync(
        string method, string path, int statusCode,
        string? userId, string? entityName, string? entityId,
        int durationMs, string? exceptionText)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            var logLevel = DetermineLogLevel(statusCode, exceptionText);
            var status   = statusCode is >= 200 and < 300 ? "SUCCESS" : "ERROR";

            await logService.CreateAsync(new LogRequest
            {
                TransactionId     = Guid.NewGuid().ToString(),
                LogLevel          = logLevel,
                Operation         = $"{method} {path}"[..Math.Min($"{method} {path}".Length, 100)],
                EntityName        = entityName,
                EntityId          = entityId,
                UserId            = userId,
                TransactionStatus = status,
                DurationMs        = durationMs,
                Logger            = nameof(ApiLoggingMiddleware),
                Message           = $"{method} {path} → {statusCode}",
                Exception         = exceptionText,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al persistir log de auditoría");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool ShouldSkip(HttpContext context)
    {
        if (context.Request.Method == HttpMethods.Options) return true;

        var path = context.Request.Path.Value ?? string.Empty;

        foreach (var prefix in ExcludedPrefixes)
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;

        return path.Contains("/hub", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/presence", StringComparison.OrdinalIgnoreCase)
            || path.Contains("/negotiate", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineLogLevel(int statusCode, string? exceptionText) =>
        exceptionText is not null ? "Fatal"
        : statusCode >= 500       ? "Error"
        : statusCode >= 400       ? "Warning"
        : "Info";

    private static string? ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // /api/{entity}/{id?} → index 1 es la entidad
        return segments.Length >= 2 ? segments[1] : null;
    }

    private static string? ExtractEntityId(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // /api/{entity}/{id} → index 2 es el id
        if (segments.Length < 3) return null;

        var candidate = segments[2];
        // Solo retorna si parece un id numérico o GUID
        return (long.TryParse(candidate, out _) || Guid.TryParse(candidate, out _))
            ? candidate
            : null;
    }
}
