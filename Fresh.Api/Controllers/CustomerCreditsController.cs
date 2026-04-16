using Fresh.Core.DTOs.CustomerCredit;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/customer-credits")]
[Authorize]
public class CustomerCreditsController : ControllerBase
{
    private readonly ICustomerCreditService _service;
    public CustomerCreditsController(ICustomerCreditService service) { _service = service; }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<CustomerCreditResponse>> GetByCustomerId(int customerId)
    {
        var credit = await _service.GetByCustomerIdAsync(customerId);
        return credit == null ? NotFound() : Ok(credit);
    }

    [HttpPost("config")]
    public async Task<ActionResult<CustomerCreditResponse>> ConfigureCredit([FromBody] CustomerCreditRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            return Ok(await _service.CreateOrUpdateConfigAsync(request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost("{id}/pay")]
    public async Task<ActionResult<CustomerCreditResponse>> RegisterPayment(int id, [FromBody] CreditPaymentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var result = await _service.RegisterPaymentAsync(id, request);
            return result == null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("customer/{customerId}/transactions")]
    public async Task<ActionResult<IEnumerable<CreditTransactionResponse>>> GetTransactions(int customerId)
    {
        return Ok(await _service.GetTransactionsAsync(customerId));
    }

    [HttpGet("customer/{customerId}/credit-orders")]
    public async Task<ActionResult<IEnumerable<CreditOrderResponse>>> GetCreditOrders(int customerId)
    {
        return Ok(await _service.GetCreditOrdersAsync(customerId));
    }

    [HttpPost("{id}/pay-orders")]
    public async Task<ActionResult<CustomerCreditResponse>> PayOrders(int id, [FromBody] PayOrdersRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            return Ok(await _service.PayOrdersAsync(id, request));
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>
    /// Reporte de pagos de deudas (Abonos) en un rango de fechas.
    /// Fechas en formato yyyy-MM-dd (hora Colombia −5).
    /// </summary>
    [HttpGet("paid-payments")]
    public async Task<ActionResult<IEnumerable<PaidDebtReportResponse>>> GetPaidPayments(
        [FromQuery] string from,
        [FromQuery] string to)
    {
        if (!DateOnly.TryParse(from, out var fromDate) || !DateOnly.TryParse(to, out var toDate))
            return BadRequest(new { message = "Fechas inválidas. Usa formato yyyy-MM-dd." });

        // Convertir fecha Colombia (UTC-5) a UTC para la búsqueda
        var fromUtc = new DateTimeOffset(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, TimeSpan.FromHours(-5));
        var toUtc   = new DateTimeOffset(toDate.Year,   toDate.Month,   toDate.Day,   23, 59, 59, TimeSpan.FromHours(-5));

        return Ok(await _service.GetPaidPaymentsAsync(fromUtc, toUtc));
    }
}