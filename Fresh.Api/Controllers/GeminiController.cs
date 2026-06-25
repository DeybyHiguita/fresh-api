using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Fresh.Api.Middleware;
using Fresh.Api.Services;
using Fresh.Core.DTOs.Log;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeminiController(
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    GoogleDriveService driveService,
    ILogService logService,
    IAppSettingsService appSettings,
    ILogger<GeminiController> logger) : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _config               = config;
    private readonly GoogleDriveService _driveService      = driveService;
    private readonly ILogService _logService               = logService;
    private readonly IAppSettingsService _appSettings      = appSettings;
    private readonly ILogger<GeminiController> _logger     = logger;

    // ── DTOs ─────────────────────────────────────────────────────────────────

    /// <summary>Producto del inventario que el frontend envía para que Gemini pueda hacer el match.</summary>
    public record ProductHint(int Id, string Name, string Unit);

    public record AnalyzeRequest(
        string Base64Image,
        string MimeType,
        string? FileName,
        string? SubFolderId,
        bool AutoMonthFolder = false,
        IEnumerable<ProductHint>? Products = null);

    public record UploadOnlyRequest(
        string Base64Image,
        string MimeType,
        string? FileName,
        string? SubFolderId,
        bool AutoMonthFolder = false);

    public record AnalyzeTextRequest(
        string TextContent,
        IEnumerable<ProductHint>? Products = null);

    public record AnalyzeDriveRequest(
        string DriveFileUrl,
        IEnumerable<ProductHint>? Products = null);

    // ── Endpoints ─────────────────────────────────────────────────────────────

    [HttpPost("analyze-invoice")]
    public async Task<IActionResult> AnalyzeInvoice([FromBody] AnalyzeRequest request)
    {
        var apiKey = await _appSettings.GetGeminiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, new { message = "Gemini API key not configured" });

        var prompt = BuildPrompt(request.Products);

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inlineData = new { mimeType = request.MimeType, data = request.Base64Image } },
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new { temperature = 0.1 }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

        var geminiTask = client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        );

        // Subida a Drive en paralelo
        var driveFileUrl = (string?)null;
        Task<string>? driveTask = null;
        try
        {
            var fileName    = request.FileName ?? $"factura_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{MimeToExtension(request.MimeType)}";
            var subFolderId = request.SubFolderId;
            if (subFolderId is null && request.AutoMonthFolder)
                subFolderId = await _driveService.GetOrCreateMonthFolderAsync();
            driveTask = _driveService.UploadInvoiceAsync(request.Base64Image, request.MimeType, fileName, subFolderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Drive not configured, skipping upload");
            await LogErrorAsync("Google Drive no configurado al analizar factura", ex.ToString());
        }

        var response     = await geminiTask;
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            await LogErrorAsync(
                $"Gemini API retornó {(int)response.StatusCode}",
                responseBody,
                transactionData: $"MimeType={request.MimeType}, FileName={request.FileName}");

            return StatusCode((int)response.StatusCode, new { message = "Error de Gemini", detail = responseBody });
        }

        if (driveTask is not null)
        {
            try   { driveFileUrl = await driveTask; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drive upload failed");
                await LogErrorAsync("Error al subir factura a Google Drive", ex.ToString(),
                    transactionData: $"FileName={request.FileName}");
            }
        }

        if (driveFileUrl is not null)
        {
            using var doc = JsonDocument.Parse(responseBody);
            var merged = doc.RootElement.EnumerateObject()
                .ToDictionary(p => p.Name, p => (object)p.Value.Clone());
            merged["driveFileUrl"] = driveFileUrl;
            return Ok(merged);
        }

        return Content(responseBody, "application/json");
    }

    [HttpPost("analyze-text-invoice")]
    public async Task<IActionResult> AnalyzeTextInvoice([FromBody] AnalyzeTextRequest request)
    {
        var apiKey = await _appSettings.GetGeminiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, new { message = "Gemini API key not configured" });

        if (string.IsNullOrWhiteSpace(request.TextContent))
            return BadRequest(new { message = "El texto de la factura no puede estar vacío" });

        var prompt = BuildPrompt(request.Products);

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = $"Texto de la factura:\n\n{request.TextContent}\n\n---\n\n{prompt}" }
                    }
                }
            },
            generationConfig = new { temperature = 0.1 }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

        var response     = await client.PostAsync(url, new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            await LogErrorAsync(
                $"Gemini text-invoice API retornó {(int)response.StatusCode}",
                responseBody,
                transactionData: $"TextLength={request.TextContent?.Length}");
            return StatusCode((int)response.StatusCode, new { message = "Error de Gemini", detail = responseBody });
        }

        return Content(responseBody, "application/json");
    }

    [HttpPost("analyze-drive-invoice")]
    public async Task<IActionResult> AnalyzeDriveInvoice([FromBody] AnalyzeDriveRequest request)
    {
        var apiKey = await _appSettings.GetGeminiApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, new { message = "Gemini API key not configured" });

        if (string.IsNullOrWhiteSpace(request.DriveFileUrl))
            return BadRequest(new { message = "URL de Drive requerida" });

        string base64Image;
        string mimeType;
        try
        {
            (base64Image, mimeType) = await _driveService.DownloadFileAsBase64Async(request.DriveFileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo descargar imagen de Drive para re-análisis");
            await LogErrorAsync("Error descargando imagen de Drive para re-análisis", ex.ToString(),
                transactionData: $"DriveUrl={request.DriveFileUrl}");
            return StatusCode(500, new { message = $"No se pudo descargar la imagen de Drive: {ex.Message}" });
        }

        var prompt = BuildPrompt(request.Products);
        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inlineData = new { mimeType, data = base64Image } },
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new { temperature = 0.1 }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";
        var response     = await client.PostAsync(url, new StringContent(System.Text.Json.JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json"));
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            await LogErrorAsync($"Gemini drive-invoice API retornó {(int)response.StatusCode}", responseBody,
                transactionData: $"DriveUrl={request.DriveFileUrl}");
            return StatusCode((int)response.StatusCode, new { message = "Error de Gemini", detail = responseBody });
        }

        return Content(responseBody, "application/json");
    }

    [HttpPost("upload-to-drive")]
    public async Task<IActionResult> UploadToDrive([FromBody] UploadOnlyRequest request)
    {
        try
        {
            var fileName    = request.FileName ?? $"archivo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{MimeToExtension(request.MimeType)}";
            var subFolderId = request.SubFolderId;
            if (subFolderId is null && request.AutoMonthFolder)
                subFolderId = await _driveService.GetOrCreateMonthFolderAsync();
            var url = await _driveService.UploadInvoiceAsync(request.Base64Image, request.MimeType, fileName, subFolderId);
            return Ok(new { driveFileUrl = url, fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drive upload-only failed");
            await LogErrorAsync("Error al subir archivo a Google Drive", ex.ToString(),
                transactionData: $"FileName={request.FileName}, MimeType={request.MimeType}");
            return StatusCode(500, new { message = "Error al subir a Google Drive", detail = ex.Message });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Construye el prompt para Gemini. Si el frontend envió el catálogo de productos,
    /// pide que la IA asigne productId a cada ítem para evitar que el frontend haga
    /// match por string (que falla con nombres abreviados o en otro idioma).
    /// </summary>
    private static string BuildPrompt(IEnumerable<ProductHint>? products)
    {
        var catalogSection = string.Empty;
        var productIdField = string.Empty;

        if (products?.Any() == true)
        {
            var lines = products.Select(p => $"  {{\"id\":{p.Id},\"nombre\":\"{p.Name}\",\"unidad\":\"{p.Unit}\"}}");
            catalogSection = $"""

Catálogo de productos existentes en el inventario:
[
{string.Join(",\n", lines)}
]

Para cada ítem de la factura busca el producto más similar en el catálogo anterior y agrega el campo "productId" con su id.
Si no encuentras coincidencia razonable usa null.
""";
            productIdField = @"      ""productId"": null,";
        }

        return $$"""
Analiza este documento (factura, recibo o PDF de compra) y extrae los datos en formato JSON estricto.
Responde ÚNICAMENTE con el JSON, sin texto adicional, sin markdown, sin bloques de código.
El documento puede ser una imagen o un PDF con múltiples páginas; analiza todo el contenido.

El JSON debe tener exactamente esta estructura:
{
  "proveedor": "nombre del proveedor o tienda",
  "numeroFactura": "número de factura si se ve",
  "fechaFactura": "fecha en formato YYYY-MM-DD",
  "subtotal": 0.00,
  "impuestos": 0.00,
  "total": 0.00,
  "items": [
    {
{{productIdField}}
      "descripcion": "nombre del producto",
      "cantidad": 1,
      "unidad": "kg/und/lt/g/lb/ml",
      "precioUnitario": 0.00,
      "precioTotal": 0.00
    }
  ]
}

Reglas:
- Si no puedes determinar un valor numérico, usa 0
- Si no hay número de factura visible, usa null
- Para la unidad intenta inferir (kg para kilos, lt para litros, und para unidades, etc.)
- Los precios deben ser numéricos sin símbolos de moneda
- La cantidad debe ser un número decimal si aplica{{catalogSection}}
""";
    }

    private async Task LogErrorAsync(string message, string exceptionText, string? transactionData = null)
    {
        try
        {
            var correlationId = HttpContext.Items[ApiLoggingMiddleware.CorrelationIdKey]?.ToString();
            await _logService.CreateAsync(new LogRequest
            {
                TransactionId     = Guid.NewGuid().ToString(),
                CorrelationId     = correlationId,
                LogLevel          = "Error",
                Operation         = $"{Request.Method} {Request.Path}",
                EntityName        = "gemini",
                UserId            = User.FindFirstValue(ClaimTypes.NameIdentifier),
                TransactionStatus = "ERROR",
                Logger            = nameof(GeminiController),
                Message           = message,
                Exception         = exceptionText,
                TransactionData   = transactionData,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "No se pudo registrar error de Gemini en logs");
        }
    }

    private static string MimeToExtension(string mimeType) => mimeType switch
    {
        "image/jpeg"       => "jpg",
        "image/png"        => "png",
        "image/webp"       => "webp",
        "image/heic"       => "heic",
        "application/pdf"  => "pdf",
        _                  => "jpg"
    };
}
