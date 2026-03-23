using Fresh.Core.DTOs.Customer;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _service;
    public CustomersController(ICustomerService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetById(int id)
    {
        var c = await _service.GetByIdAsync(id);
        return c == null ? NotFound() : Ok(c);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create([FromBody] CustomerRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var c = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = c.Id }, c);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, [FromBody] CustomerRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var c = await _service.UpdateAsync(id, request);
            return c == null ? NotFound() : Ok(c);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id) => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}