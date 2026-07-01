using Fresh.Core.DTOs.Investment;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvestmentsController : ControllerBase
{
    private readonly IInvestmentService _investmentService;

    public InvestmentsController(IInvestmentService investmentService)
    {
        _investmentService = investmentService;
    }

    // ── Investments ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var investments = await _investmentService.GetAllAsync();
        return Ok(investments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var investment = await _investmentService.GetByIdAsync(id);
        if (investment == null) return NotFound();
        return Ok(investment);
    }

    [HttpGet("investor/{investorId}")]
    public async Task<IActionResult> GetByInvestor(int investorId)
    {
        var investments = await _investmentService.GetByInvestorAsync(investorId);
        return Ok(investments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InvestmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var investment = await _investmentService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = investment.Id }, investment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] InvestmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var investment = await _investmentService.UpdateAsync(id, request);
            if (investment == null) return NotFound();
            return Ok(investment);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _investmentService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // ── Investment Items ─────────────────────────────────────────────────────

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] InvestmentItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var item = await _investmentService.AddItemAsync(request);
            return Ok(item);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int itemId, [FromBody] InvestmentItemRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var item = await _investmentService.UpdateItemAsync(itemId, request);
            if (item == null) return NotFound();
            return Ok(item);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/import-need-items")]
    public async Task<IActionResult> ImportNeedItems(int id)
    {
        try
        {
            var investment = await _investmentService.ImportNeedItemsAsync(id);
            return Ok(investment);
        }
        catch (KeyNotFoundException ex)   { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var deleted = await _investmentService.RemoveItemAsync(itemId);
        if (!deleted) return NotFound();
        return NoContent();
    }

    // ── Status ───────────────────────────────────────────────────────────────

    [HttpPost("{id}/activate")]
    public async Task<IActionResult> Activate(int id)
    {
        var investment = await _investmentService.GetByIdAsync(id);
        if (investment == null) return NotFound();

        var request = new InvestmentRequest
        {
            InvestorId      = investment.InvestorId,
            Amount          = investment.Amount,
            InvestmentDate  = investment.InvestmentDate,
            Description     = investment.Description,
            Status          = "Activo",
        };

        var updated = await _investmentService.UpdateAsync(id, request);
        return Ok(updated);
    }
}
