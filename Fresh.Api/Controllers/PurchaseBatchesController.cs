using Fresh.Api.Services;
using Fresh.Core.DTOs.PurchaseBatch;
using Fresh.Core.DTOs.PurchaseDetail;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PurchaseBatchesController : ControllerBase
{
    private readonly IPurchaseBatchService _batchService;
    private readonly ShareTokenService _tokens;

    public PurchaseBatchesController(IPurchaseBatchService batchService, ShareTokenService tokens)
    {
        _batchService = batchService;
        _tokens       = tokens;
    }

    private int StoreId => int.TryParse(User.FindFirst("store_id")?.Value, out var id) ? id : 0;

    /// <summary>
    /// Obtiene lotes de compra con paginación (skip/take). Si no se pasan parámetros devuelve todos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? skip, [FromQuery] int? take)
    {
        if (skip.HasValue && take.HasValue)
        {
            var (items, total) = await _batchService.GetPagedAsync(skip.Value, take.Value, StoreId);
            return Ok(new { items, total });
        }

        var batches = await _batchService.GetAllAsync(StoreId);
        return Ok(batches);
    }

    /// <summary>
    /// Listado liviano (sin detalles) con total precalculado en la DB.
    /// Ideal para selectores y dropdowns. Soporta búsqueda por nombre.
    /// </summary>
    [HttpGet("summaries")]
    public async Task<IActionResult> GetSummaries(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 5,
        [FromQuery] string? search = null,
        [FromQuery] int? keepExpenseId = null)
    {
        var (items, total) = await _batchService.GetSummariesAsync(skip, take, search, keepExpenseId, StoreId);
        return Ok(new { items, total });
    }

    /// <summary>
    /// Obtiene un lote de compra por ID con sus detalles
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PurchaseBatchResponse>> GetById(int id)
    {
        var batch = await _batchService.GetByIdAsync(id);
        if (batch == null)
            return NotFound(new { message = "Lote de compra no encontrado" });

        return Ok(batch);
    }

    /// <summary>
    /// Crea un nuevo lote de compra
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PurchaseBatchResponse>> Create([FromBody] PurchaseBatchRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var batch = await _batchService.CreateAsync(request, StoreId);
            return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un lote de compra existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PurchaseBatchResponse>> Update(int id, [FromBody] PurchaseBatchRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var batch = await _batchService.UpdateAsync(id, request);
            if (batch == null)
                return NotFound(new { message = "Lote de compra no encontrado" });

            return Ok(batch);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un lote de compra y sus detalles (CASCADE)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _batchService.DeleteAsync(id);
        if (!result)
            return NotFound(new { message = "Lote de compra no encontrado" });

        return NoContent();
    }

    // ?? Detalle de compra (anidado bajo el lote) ??????????????????????????????

    /// <summary>
    /// Agrega un producto al lote de compra y actualiza el stock
    /// </summary>
    [HttpPost("{id}/details")]
    public async Task<ActionResult<PurchaseDetailResponse>> AddDetail(int id, [FromBody] PurchaseDetailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var detail = await _batchService.AddDetailAsync(id, request);
            return CreatedAtAction(nameof(GetById), new { id }, detail);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un detalle del lote y recalcula el stock del producto
    /// </summary>
    [HttpPut("{id}/details/{detailId}")]
    public async Task<ActionResult<PurchaseDetailResponse>> UpdateDetail(
        int id, int detailId, [FromBody] PurchaseDetailRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var detail = await _batchService.UpdateDetailAsync(id, detailId, request);
            if (detail == null)
                return NotFound(new { message = "Detalle de compra no encontrado" });

            return Ok(detail);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un detalle del lote y revierte el stock del producto
    /// </summary>
    [HttpDelete("{id}/details/{detailId}")]
    public async Task<IActionResult> RemoveDetail(int id, int detailId)
    {
        var result = await _batchService.RemoveDetailAsync(id, detailId);
        if (!result)
            return NotFound(new { message = "Detalle de compra no encontrado" });

        return NoContent();
    }

    // ── Facturas escaneadas del lote ─────────────────────────────────────────

    /// <summary>Lista las facturas escaneadas asociadas al lote.</summary>
    [HttpGet("{id}/invoices")]
    public async Task<ActionResult<IEnumerable<PurchaseBatchInvoiceResponse>>> GetInvoices(int id)
        => Ok(await _batchService.GetInvoicesAsync(id));

    /// <summary>Registra una factura escaneada en el lote.</summary>
    [HttpPost("{id}/invoices")]
    public async Task<ActionResult<PurchaseBatchInvoiceResponse>> AddInvoice(int id, [FromBody] PurchaseBatchInvoiceRequest request)
    {
        try
        {
            var invoice = await _batchService.AddInvoiceAsync(id, request);
            return CreatedAtAction(nameof(GetById), new { id }, invoice);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Elimina una factura escaneada del lote.</summary>
    [HttpDelete("{id}/invoices/{invoiceId}")]
    public async Task<IActionResult> RemoveInvoice(int id, int invoiceId)
    {
        var result = await _batchService.RemoveInvoiceAsync(id, invoiceId);
        if (!result) return NotFound(new { message = "Factura no encontrada" });
        return NoContent();
    }

    // ── Compartir lote (enlace público) ──────────────────────────────────────

    /// <summary>
    /// Genera un token cifrado (AES-256) para compartir el lote sin autenticación.
    /// </summary>
    [HttpGet("{id}/share-token")]
    public async Task<IActionResult> GetShareToken(int id)
    {
        var exists = await _batchService.GetByIdAsync(id);
        if (exists == null)
            return NotFound(new { message = "Lote no encontrado" });

        return Ok(new { token = _tokens.Protect(id) });
    }

    /// <summary>
    /// Consulta pública del lote usando un token cifrado. No requiere autenticación.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("public/{token}")]
    public async Task<ActionResult<PublicBatchResponse>> GetPublic(string token)
    {
        var batchId = _tokens.Unprotect(token);
        if (batchId is null)
            return BadRequest(new { message = "Enlace inválido o expirado" });

        var batch = await _batchService.GetByIdAsync(batchId.Value);
        if (batch is null)
            return NotFound(new { message = "Lote no encontrado" });

        var response = new PublicBatchResponse
        {
            BatchName = batch.BatchName,
            StartDate = batch.StartDate,
            EndDate   = batch.EndDate,
            Total     = batch.Details.Sum(d => d.TotalValue),
            Items     = batch.Details
                .OrderBy(d => d.ProductName)
                .Select(d => new PublicBatchItem
                {
                    ProductName = d.ProductName,
                    ProductUnit = d.ProductUnit,
                    Quantity    = d.Quantity,
                    UnitPrice   = d.UnitPrice,
                    TotalValue  = d.TotalValue,
                })
                .ToList(),
        };

        return Ok(response);
    }
}
