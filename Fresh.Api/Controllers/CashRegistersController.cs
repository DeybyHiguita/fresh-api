using Fresh.Core.DTOs.CashRegister;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/cash-registers")]
[Authorize]
public class CashRegistersController : ControllerBase
{
    private readonly ICashRegisterService _service;
    public CashRegistersController(ICashRegisterService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CashRegisterResponse>>> GetAll([FromQuery] int? periodId)
        => Ok(await _service.GetAllAsync(periodId));

    [HttpGet("{id}")]
    public async Task<ActionResult<CashRegisterResponse>> GetById(int id)
    {
        var reg = await _service.GetByIdAsync(id);
        return reg == null ? NotFound() : Ok(reg);
    }

    [HttpGet("{id}/system-totals")]
    public async Task<ActionResult<CashSystemTotalsResponse>> GetSystemTotals(int id)
    {
        var totals = await _service.GetSystemTotalsAsync(id);
        return totals == null ? NotFound() : Ok(totals);
    }

    [HttpPost("open")]
    public async Task<ActionResult<CashRegisterResponse>> Open([FromBody] OpenCashRegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reg = await _service.OpenRegisterAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = reg.Id }, reg);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<CashRegisterResponse>> Close(int id, [FromBody] CloseCashRegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reg = await _service.CloseRegisterAsync(id, request);
            return reg == null ? NotFound() : Ok(reg);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/edit")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CashRegisterResponse>> Edit(int id, [FromBody] EditCashRegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reg = await _service.EditAsync(id, request);
            return reg == null ? NotFound() : Ok(reg);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/opening-balance")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<CashRegisterResponse>> UpdateOpeningBalance(int id, [FromBody] UpdateOpeningBalanceRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var reg = await _service.UpdateOpeningBalanceAsync(id, request.OpeningBalance);
            return reg == null ? NotFound() : Ok(reg);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
