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

    // Rutas que no se loguean para evitar recursión
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
        var path = context.Request.Path.Value ?? string.Empty;

        // Omitir rutas excluidas
        if (ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var transactionId = Guid.NewGuid().ToString();
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();
        var startTime = DateTimeOffset.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Habilitar lectura del request body (necesario para auth)
        context.Request.EnableBuffering();

        // Leer body del request para extraer datos de auth
        var requestBody = await ReadRequestBodyAsync(context.Request);

        // Sustituir el response stream para poder leer el body de la respuesta
        var originalResponseBody = context.Response.Body;
        using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        string? exceptionMessage = null;
        string logLevel = "INFO";

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            exceptionMessage = $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            logLevel = "ERROR";
            throw; // Re-lanzar para no ocultar el error
        }
        finally
        {
            stopwatch.Stop();

            // Leer el body de la respuesta antes de devolverlo al cliente
            responseBuffer.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBuffer).ReadToEndAsync();

            // Devolver el stream original al cliente
            responseBuffer.Seek(0, SeekOrigin.Begin);
            await responseBuffer.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            var statusCode = context.Response.StatusCode;

            // Determinar nivel de log según status code
            if (exceptionMessage == null)
            {
                logLevel = statusCode switch
                {
                    >= 500 => "ERROR",
                    >= 400 => "WARN",
                    _      => "INFO"
                };
            }

            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var method = context.Request.Method;
            var operation = $"{method} {path}";

            // Extraer nombre de entidad desde la ruta: /api/{entidad}/...
            var entityName = ExtractEntityName(path);

            // Extraer ID de entidad desde la ruta si existe
            var entityId = ExtractEntityId(path, requestBody, entityName);

            var transactionStatus = statusCode switch
            {
                >= 200 and < 300 => "SUCCESS",
                >= 400 and < 500 => "CLIENT_ERROR",
                >= 500            => "SERVER_ERROR",
                _                 => "UNKNOWN"
            };

            // Para errores, enriquecer el mensaje con el body de la respuesta
            var errorDetail = ExtractErrorMessage(responseBody, statusCode);
            var message = errorDetail is not null
                ? $"{operation} ? {transactionStatus} ({stopwatch.ElapsedMilliseconds} ms) | {errorDetail}"
                : $"{operation} ? {transactionStatus} ({stopwatch.ElapsedMilliseconds} ms)";

            await SaveLogAsync(
                transactionId,
                correlationId,
                startTime,
                logLevel,
                operation,
                entityName,
                entityId,
                userId,
                transactionStatus,
                stopwatch.ElapsedMilliseconds,
                message,
                exceptionMessage
            );
        }
    }

    private async Task SaveLogAsync(
        string transactionId,
        string correlationId,
        DateTimeOffset logDate,
        string logLevel,
        string operation,
        string? entityName,
        string? entityId,
        string? userId,
        string transactionStatus,
        long durationMs,
        string message,
        string? exception)
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
            // Nunca dejar que el logging rompa la respuesta al cliente
            _logger.LogError(ex, "Error al guardar log en base de datos");
        }
    }

    // ?? Helpers ??????????????????????????????????????????????????????????????

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength is null or 0)
            return null;

        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    /// <summary>
    /// Para rutas /api/auth/*, extrae el email del body JSON como EntityId.
    /// Para el resto, usa el tercer segmento de la ruta (id numérico).
    /// </summary>
    private static string? ExtractEntityId(string path, string? requestBody, string? entityName)
    {
        if (entityName?.Equals("auth", StringComparison.OrdinalIgnoreCase) == true
            && requestBody is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(requestBody);
                if (doc.RootElement.TryGetProperty("email", out var emailProp))
                    return emailProp.GetString();
            }
            catch
            {
                // body no es JSON válido, ignorar
            }
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 3 ? segments[2] : null;
    }

    /// <summary>
    /// Extrae el nombre de la entidad desde la ruta. Ej: /api/recipes/5 ? "recipes"
    /// </summary>
    private static string? ExtractEntityName(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length >= 2 ? segments[1] : null;
    }

    /// <summary>
    /// Extrae el campo "message" del JSON de respuesta para errores 4xx/5xx.
    /// </summary>
    private static string? ExtractErrorMessage(string responseBody, int statusCode)
    {
        if (statusCode < 400 || string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("message", out var msg))
                return msg.GetString();
        }
        catch
        {
            // respuesta no es JSON (ej: HTML de error), ignorar
        }

        return null;
    }
}
