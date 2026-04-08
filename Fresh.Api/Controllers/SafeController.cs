using Fresh.Core.DTOs.Safe;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/safe")]
[Authorize]
public class SafeController : ControllerBase
{
    private readonly ISafeService _service;
    public SafeController(ISafeService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<SafeResponse>> Get([FromQuery] string safeType = "caja_fuerte")
        => Ok(await _service.GetSafeAsync(safeType));

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<SafeTransactionResponse>>> GetTransactions(
        [FromQuery] string safeType = "caja_fuerte", [FromQuery] int? limit = null)
        => Ok(await _service.GetTransactionsAsync(safeType, limit));

    [HttpPost("expense")]
    public async Task<ActionResult<SafeTransactionResponse>> AddExpense([FromBody] SafeExpenseRequest request)
    {
        try { return Ok(await _service.AddExpenseAsync(request)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message }); }
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<SafeTransactionResponse>> AddDeposit([FromBody] SafeDepositRequest request)
    {
        try { return Ok(await _service.AddDepositAsync(request)); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message }); }
    }
}
