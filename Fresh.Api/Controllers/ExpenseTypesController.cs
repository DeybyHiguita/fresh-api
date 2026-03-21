using Fresh.Core.DTOs.ExpenseType;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/expense-types")]
[Authorize] // Restringimos todo el controlador
public class ExpenseTypesController : ControllerBase
{
    private readonly IExpenseTypeService _service;
    public ExpenseTypesController(IExpenseTypeService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExpenseTypeResponse>>> GetAll([FromQuery] bool onlyActive = true)
    {
        return Ok(await _service.GetAllAsync(onlyActive));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExpenseTypeResponse>> GetById(int id)
    {
        var type = await _service.GetByIdAsync(id);
        if (type == null) return NotFound();
        return Ok(type);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseTypeResponse>> Create([FromBody] ExpenseTypeRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var type = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExpenseTypeResponse>> Update(int id, [FromBody] ExpenseTypeRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var type = await _service.UpdateAsync(id, request);
        if (type == null) return NotFound();
        return Ok(type);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }
}