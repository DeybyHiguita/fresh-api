using Fresh.Core.DTOs.Expense;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;
    public ExpensesController(IExpenseService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("by-date")]
    public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetByMonthYear([FromQuery] int month, [FromQuery] int year)
    {
        if (month < 1 || month > 12 || year < 2000)
            return BadRequest(new { message = "Mes o año inválido" });
        var result = await _service.GetByMonthYearAsync(month, year);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseResponse>> GetById(int id)
    {
        var expense = await _service.GetByIdAsync(id);
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseResponse>> Create([FromBody] ExpenseRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var expense = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseResponse>> Update(int id, [FromBody] ExpenseRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var expense = await _service.UpdateAsync(id, request);
            if (expense == null) return NotFound();
            return Ok(expense);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}