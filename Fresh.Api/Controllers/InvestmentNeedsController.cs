using Fresh.Core.DTOs.Investment;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/investment-needs")]
public class InvestmentNeedsController : ControllerBase
{
    private readonly IInvestmentNeedService _needService;

    public InvestmentNeedsController(IInvestmentNeedService needService)
    {
        _needService = needService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var needs = await _needService.GetAllAsync();
        return Ok(needs);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var need = await _needService.GetByIdAsync(id);
        if (need == null) return NotFound();
        return Ok(need);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] InvestmentNeedRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var need = await _needService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = need.Id }, need);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] InvestmentNeedRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var need = await _needService.UpdateAsync(id, request);
            return Ok(need);
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _needService.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var investments = await _needService.ApproveAsync(id);
            return Ok(investments);
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

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        try
        {
            var rejected = await _needService.RejectAsync(id);
            if (!rejected) return NotFound();
            return Ok(new { message = "Solicitud rechazada" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
