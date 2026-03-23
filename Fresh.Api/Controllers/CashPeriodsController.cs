using Fresh.Core.DTOs.CashPeriod;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/cash-periods")]
[Authorize]
public class CashPeriodsController : ControllerBase
{
    private readonly ICashPeriodService _service;
    public CashPeriodsController(ICashPeriodService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashPeriodResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<CashPeriodResponse>> GetById(int id)
    {
        var period = await _service.GetByIdAsync(id);
        return period == null ? NotFound() : Ok(period);
    }

    [HttpPost]
    public async Task<ActionResult<CashPeriodResponse>> Create([FromBody] CashPeriodRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var period = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = period.Id }, period);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/close")]
    public async Task<ActionResult<CashPeriodResponse>> ClosePeriod(int id)
    {
        try
        {
            var period = await _service.ClosePeriodAsync(id);
            return period == null ? NotFound() : Ok(period);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
