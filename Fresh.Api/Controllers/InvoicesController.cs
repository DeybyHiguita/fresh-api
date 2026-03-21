using Fresh.Core.DTOs.Invoice;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InvoiceResponse>>> GetAll()
    {
        return Ok(await _invoiceService.GetAllAsync());
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<InvoiceResponse>> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    [HttpGet("order/{orderId}")]
    [Authorize]
    public async Task<ActionResult<InvoiceResponse>> GetByOrderId(int orderId)
    {
        var invoice = await _invoiceService.GetByOrderIdAsync(orderId);
        if (invoice == null) return NotFound(new { message = "No se encontró factura para esta orden." });
        return Ok(invoice);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<InvoiceResponse>> Create([FromBody] InvoiceRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var invoice = await _invoiceService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
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
}