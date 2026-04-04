using Fresh.Core.DTOs.PurchaseDetail;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseDetailsController : ControllerBase
{
    private readonly IPurchaseBatchService _batchService;

    public PurchaseDetailsController(IPurchaseBatchService batchService)
    {
        _batchService = batchService;
    }

    /// <summary>
    /// Actualiza los precios de varios detalles de compra en un solo request.
    /// Usado para registrar los precios reales al momento de comprar en el lote.
    /// </summary>
    [HttpPut("batch-update")]
    public async Task<IActionResult> BatchUpdate([FromBody] BatchUpdateRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (request.Updates == null || request.Updates.Count == 0)
            return BadRequest(new { message = "Debe incluir al menos un ítem para actualizar" });

        await _batchService.BatchUpdateDetailsAsync(request.Updates);
        return NoContent();
    }

    /// <summary>
    /// Obtiene el historial de precios de un producto en todos los lotes anteriores.
    /// Permite ver cuánto costó ese producto en ocasiones pasadas.
    /// </summary>
    [HttpGet("product/{productId}/price-history")]
    public async Task<ActionResult<IEnumerable<ProductPriceHistoryResponse>>> GetPriceHistory(int productId)
    {
        var history = await _batchService.GetProductPriceHistoryAsync(productId);
        return Ok(history);
    }
}
