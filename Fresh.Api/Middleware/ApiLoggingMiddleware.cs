using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fresh.Core.DTOs.Log;
using Fresh.Core.Interfaces;

namespace Fresh.Api.Middleware;

/// <summary>
/// Responsabilidad única: registrar cada petición HTTP con duración, entidad,
/// usuario, excepción (si la hubo) y cuerpo de respuesta para errores.
/// Para errores 4xx/5xx intercambia el Response.Body por un buffer para poder
/// leer el JSON de error sin afectar al cliente.
/// Depende de ILogService (abstracción, no FreshDbContext).
/// </summary>
public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    // Clave pública para que controllers puedan leer/escribir el correlationId
    public const string CorrelationIdKey = "_correlationId";

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

        // Asignar correlationId al inicio para que controllers puedan enlazar sus propios logs
        var correlationId = Guid.NewGuid().ToString();
        context.Items[CorrelationIdKey] = correlationId;

        context.Request.EnableBuffering();

        // Sustituir el stream de respuesta por un buffer propio para poder leerlo en errores
        var originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

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

            // Devolver el buffer al cliente ANTES de leerlo para logging
            responseBuffer.Seek(0, SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalBody);
            context.Response.Body = originalBody;

            var statusCode = capturedException is not null ? 500 : context.Response.StatusCode;
            var method     = context.Request.Method;
            var path       = context.Request.Path.Value ?? string.Empty;
            var userId     = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var entityName = ExtractEntityName(path);
            var entityId   = ExtractEntityId(path);
            var durationMs = (int)Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue);
            var exText     = capturedException?.ToString();

            // Capturar body de respuesta solo para errores (no penalizar el caso feliz)
            string? responseBodyText = null;
            if (statusCode >= 400 && responseBuffer.Length > 0)
            {
                responseBuffer.Seek(0, SeekOrigin.Begin);
                responseBodyText = await new StreamReader(responseBuffer, Encoding.UTF8, leaveOpen: true).ReadToEndAsync();
            }

            var message = BuildMessage(method, path, statusCode, responseBodyText);

            _ = Task.Run(async () =>
                await PersistLogAsync(
                    correlationId, method, path, statusCode,
                    userId, entityName, entityId, durationMs,
                    exText, message, responseBodyText));
        }
    }

    // ── Persistencia ──────────────────────────────────────────────────────────

    private async Task PersistLogAsync(
        string correlationId,
        string method, string path, int statusCode,
        string? userId, string? entityName, string? entityId,
        int durationMs, string? exceptionText,
        string message, string? transactionData)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var logService = scope.ServiceProvider.GetRequiredService<ILogService>();

            await logService.CreateAsync(new LogRequest
            {
                TransactionId     = Guid.NewGuid().ToString(),
                CorrelationId     = correlationId,
                LogLevel          = DetermineLogLevel(statusCode, exceptionText),
                Operation         = Truncate($"{method} {path}", 100),
                EntityName        = entityName,
                EntityId          = entityId,
                UserId            = userId,
                TransactionStatus = statusCode is >= 200 and < 300 ? "SUCCESS" : "ERROR",
                DurationMs        = durationMs,
                Logger            = nameof(ApiLoggingMiddleware),
                Message           = message,
                Exception         = exceptionText,
                TransactionData   = transactionData,
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

        return path.Contains("/hub",       StringComparison.OrdinalIgnoreCase)
            || path.Contains("/presence",  StringComparison.OrdinalIgnoreCase)
            || path.Contains("/negotiate", StringComparison.OrdinalIgnoreCase);
    }

    private static string DetermineLogLevel(int statusCode, string? exceptionText) =>
        exceptionText is not null ? "Fatal"
        : statusCode >= 500       ? "Error"
        : statusCode >= 400       ? "Warning"
        : "Info";

    /// <summary>
    /// Construye un mensaje legible: si el body de error tiene campo "message", lo usa.
    /// </summary>
    private static string BuildMessage(string method, string path, int statusCode, string? responseBody)
    {
        if (statusCode < 400 || string.IsNullOrWhiteSpace(responseBody))
            return $"{method} {path} → {statusCode}";

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            // Intentar extraer un mensaje legible del cuerpo
            var parts = new List<string>();

            if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                parts.Add(msg.GetString()!);

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
            {
                // "detail" puede ser un JSON anidado de Gemini; extraer solo el mensaje
                var detailStr = detail.GetString()!;
                try
                {
                    using var innerDoc = JsonDocument.Parse(detailStr);
                    if (innerDoc.RootElement.TryGetProperty("error", out var err)
                        && err.TryGetProperty("message", out var errMsg))
                        parts.Add(errMsg.GetString()!);
                    else
                        parts.Add(Truncate(detailStr, 200));
                }
                catch
                {
                    parts.Add(Truncate(detailStr, 200));
                }
            }

            return parts.Count > 0
                ? $"[{statusCode}] {string.Join(" — ", parts)}"
                : $"{method} {path} → {statusCode}";
        }
        catch
        {
            return $"{method} {path} → {statusCode}";
        }
    }

    private static string? ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : null;
    }

    private static string? ExtractEntityId(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3) return null;
        var candidate = segments[2];
        return (long.TryParse(candidate, out _) || Guid.TryParse(candidate, out _))
            ? candidate : null;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
