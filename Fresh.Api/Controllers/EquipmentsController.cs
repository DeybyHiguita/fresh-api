using Fresh.Core.DTOs.Equipment;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipmentsController : ControllerBase
{
    private readonly IEquipmentService _service;
    public EquipmentsController(IEquipmentService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentResponse>>> GetAll([FromQuery] string? status)
        => Ok(await _service.GetAllAsync(status));

    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentResponse>> GetById(int id)
    {
        var eq = await _service.GetByIdAsync(id);
        return eq == null ? NotFound() : Ok(eq);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentResponse>> Create([FromBody] EquipmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var eq = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = eq.Id }, eq);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EquipmentResponse>> Update(int id, [FromBody] EquipmentRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var eq = await _service.UpdateAsync(id, request);
            return eq == null ? NotFound() : Ok(eq);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<EquipmentResponse>> UpdateStatus(int id, [FromBody] string newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus)) return BadRequest(new { message = "Estado inválido." });
        var eq = await _service.UpdateStatusAsync(id, newStatus);
        return eq == null ? NotFound() : Ok(eq);
    }
}
