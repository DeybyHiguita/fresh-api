using Fresh.Core.DTOs.Investment;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class ProfitDistributionsController : ControllerBase
{
    private readonly IProfitDistributionService _service;

    public ProfitDistributionsController(IProfitDistributionService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProfitDistributionResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<ProfitDistributionResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound(new { message = "Distribución no encontrada" });
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ProfitDistributionResponse>> Create([FromBody] ProfitDistributionRequest request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0";
        var userId    = int.TryParse(userIdStr, out var id) ? id : 0;
        var result    = await _service.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Distribución no encontrada" });
        }
    }
}
