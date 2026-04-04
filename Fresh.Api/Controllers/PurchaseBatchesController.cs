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

    public PurchaseBatchesController(IPurchaseBatchService batchService)
    {
        _batchService = batchService;
    }

    /// <summary>
    /// Obtiene lotes de compra con paginación (skip/take). Si no se pasan parámetros devuelve todos.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? skip, [FromQuery] int? take)
    {
        if (skip.HasValue && take.HasValue)
        {
            var (items, total) = await _batchService.GetPagedAsync(skip.Value, take.Value);
            return Ok(new { items, total });
        }

        var batches = await _batchService.GetAllAsync();
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
        [FromQuery] string? search = null)
    {
        var (items, total) = await _batchService.GetSummariesAsync(skip, take, search);
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
            var batch = await _batchService.CreateAsync(request);
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
}
