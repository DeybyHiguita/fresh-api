using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fresh.Core.Entities;
using Fresh.Infrastructure.Data;

namespace Fresh.Api.Middleware;

public class ApiLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiLoggingMiddleware> _logger;

    private static readonly string[] ExcludedPaths =
    [
        "/api/logs",
        "/swagger",
        "/favicon.ico"
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
        // 1. BYPASS PREFLIGHT: Ignorar peticiones OPTIONS de CORS inmediatamente
        if (context.Request.Method == HttpMethods.Options)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;

        if (path.Contains("/hub") || path.Contains("/presence") || path.Contains("/negotiate"))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        try
        {
            await _next(context);
        }
        finally
        {
            // 2. EXTRAER LOS DATOS AQUÍ. El HttpContext sigue vivo en este punto.
            var method = context.Request.Method;
            var statusCode = context.Response.StatusCode;
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 3. Pasar SOLO variables simples al Task.Run, NUNCA el context.
            _ = Task.Run(async () => {
                await SaveSimpleLogAsync(method, path, statusCode, userId);
            });
        }
    }

    private async Task SaveSimpleLogAsync(string method, string path, int statusCode, string? userId)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FreshDbContext>();

            var log = new Log
            {
                TransactionId = Guid.NewGuid().ToString(),
                LogDate = DateTimeOffset.UtcNow,
                LogLevel = statusCode >= 400 ? "ERROR" : "INFO",
                Operation = $"{method} {path}", // Usar variable extraída
                TransactionStatus = statusCode >= 200 && statusCode < 300 ? "SUCCESS" : "ERROR",
                DurationMs = 0,
                UserId = userId, // Usar variable extraída
                Message = $"Respuesta rápida: {statusCode}",
                CreatedAt = DateTimeOffset.UtcNow,
                Logger = nameof(ApiLoggingMiddleware)
            };

            dbContext.Logs.Add(log);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en el log simplificado");
        }
    }

    private async Task SaveLogAsync(
        string transactionId, string correlationId, DateTimeOffset logDate, string logLevel, string operation,
        string? entityName, string? entityId, string? userId, string transactionStatus, long durationMs,
        string message, string? exception)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FreshDbContext>();

            var log = new Log
            {
                TransactionId = transactionId,
                CorrelationId = correlationId,
                LogDate = logDate,
                LogLevel = logLevel,
                Operation = operation,
                EntityName = entityName,
                EntityId = entityId,
                UserId = userId,
                TransactionStatus = transactionStatus,
                DurationMs = (int)Math.Min(durationMs, int.MaxValue),
                Logger = nameof(ApiLoggingMiddleware),
                Message = message,
                Exception = exception,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Logs.Add(log);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar log en base de datos");
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0) return null;
        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static string? ExtractEntityId(string path, string? requestBody, string? entityName)
    {
        if (entityName?.Equals("auth", StringComparison.OrdinalIgnoreCase) == true && requestBody is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.TryGetProperty("email", out var emailProp)) return emailProp.GetString();
            }
            catch { }
        }
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3 ? segments[2] : null;
    }

    private static string? ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : null;
    }

    private static string? ExtractErrorMessage(string responseBody, int statusCode)
    {
        if (statusCode < 400 || string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var msg)) return msg.GetString();
        }
        catch { }
        return null;
    }
}