using Fresh.Core.DTOs.EquipmentCategory;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/equipment-categories")]
[Authorize]
public class EquipmentCategoriesController : ControllerBase
{
    private readonly IEquipmentCategoryService _service;
    public EquipmentCategoriesController(IEquipmentCategoryService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EquipmentCategoryResponse>>> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    [HttpGet("{id}")]
    public async Task<ActionResult<EquipmentCategoryResponse>> GetById(int id)
    {
        var cat = await _service.GetByIdAsync(id);
        return cat == null ? NotFound() : Ok(cat);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentCategoryResponse>> Create([FromBody] EquipmentCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var cat = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = cat.Id }, cat);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<EquipmentCategoryResponse>> Update(int id, [FromBody] EquipmentCategoryRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var cat = await _service.UpdateAsync(id, request);
        return cat == null ? NotFound() : Ok(cat);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
