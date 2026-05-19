using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using Fresh.Api.Services;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GeminiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly GoogleDriveService _driveService;
    private readonly ILogger<GeminiController> _logger;

    public GeminiController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        GoogleDriveService driveService,
        ILogger<GeminiController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _driveService = driveService;
        _logger = logger;
    }

    public record AnalyzeRequest(string Base64Image, string MimeType, string? FileName);

    [HttpPost("analyze-invoice")]
    public async Task<IActionResult> AnalyzeInvoice([FromBody] AnalyzeRequest request)
    {
        var apiKey = _config["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return StatusCode(500, new { message = "Gemini API key not configured" });

        var prompt = @"Analiza esta imagen de factura/recibo de compra y extrae los datos en formato JSON estricto.
Responde ÚNICAMENTE con el JSON, sin texto adicional, sin markdown, sin bloques de código.

El JSON debe tener exactamente esta estructura:
{
  ""proveedor"": ""nombre del proveedor o tienda"",
  ""numeroFactura"": ""número de factura si se ve"",
  ""fechaFactura"": ""fecha en formato YYYY-MM-DD"",
  ""subtotal"": 0.00,
  ""impuestos"": 0.00,
  ""total"": 0.00,
  ""items"": [
    {
      ""descripcion"": ""nombre del producto"",
      ""cantidad"": 1,
      ""unidad"": ""kg/und/lt/g/lb/ml"",
      ""precioUnitario"": 0.00,
      ""precioTotal"": 0.00
    }
  ]
}

Reglas:
- Si no puedes determinar un valor numérico, usa 0
- Si no hay número de factura visible, usa null
- Para la unidad intenta inferir (kg para kilos, lt para litros, und para unidades, etc.)
- Los precios deben ser numéricos sin símbolos de moneda
- La cantidad debe ser un número decimal si aplica";

        var body = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            inlineData = new
                            {
                                mimeType = request.MimeType,
                                data = request.Base64Image
                            }
                        },
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new { temperature = 0.1 }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

        // Run Gemini analysis and Google Drive upload in parallel
        var geminiTask = client.PostAsync(
            url,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        );

        var driveFileUrl = (string?)null;
        Task<string>? driveTask = null;
        try
        {
            var fileName = request.FileName ?? $"factura_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{MimeToExtension(request.MimeType)}";
            driveTask = _driveService.UploadInvoiceAsync(request.Base64Image, request.MimeType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google Drive not configured, skipping upload");
        }

        var response = await geminiTask;
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return StatusCode((int)response.StatusCode, new { message = "Error de Gemini", detail = responseBody });

        if (driveTask is not null)
        {
            try { driveFileUrl = await driveTask; }
            catch (Exception ex) { _logger.LogWarning(ex, "Drive upload failed"); }
        }

        // Merge Drive URL into the response JSON
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

    public record UploadOnlyRequest(string Base64Image, string MimeType, string? FileName);

    [HttpPost("upload-to-drive")]
    public async Task<IActionResult> UploadToDrive([FromBody] UploadOnlyRequest request)
    {
        try
        {
            var fileName = request.FileName ?? $"archivo_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{MimeToExtension(request.MimeType)}";
            var url = await _driveService.UploadInvoiceAsync(request.Base64Image, request.MimeType, fileName);
            return Ok(new { driveFileUrl = url, fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drive upload-only failed");
            return StatusCode(500, new { message = "Error al subir a Google Drive", detail = ex.Message });
        }
    }

    private static string MimeToExtension(string mimeType) => mimeType switch
    {
        "image/jpeg" => "jpg",
        "image/png"  => "png",
        "image/webp" => "webp",
        "image/heic" => "heic",
        _            => "jpg"
    };
}
