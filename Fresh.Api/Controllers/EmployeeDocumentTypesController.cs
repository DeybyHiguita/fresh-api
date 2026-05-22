using Fresh.Core.DTOs.EmployeeDocumentType;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeeDocumentTypesController : ControllerBase
{
    private readonly IEmployeeDocumentTypeService _service;

    public EmployeeDocumentTypesController(IEmployeeDocumentTypeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Obtiene todos los tipos de documento
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDocumentTypeResponse>>> GetAll()
    {
        var types = await _service.GetAllAsync();
        return Ok(types);
    }

    /// <summary>
    /// Obtiene tipos de documento activos
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<EmployeeDocumentTypeResponse>>> GetActive()
    {
        var types = await _service.GetActiveAsync();
        return Ok(types);
    }

    /// <summary>
    /// Obtiene tipos de documento por a quién aplica (employee/child)
    /// </summary>
    [HttpGet("by-applies-to/{appliesTo}")]
    public async Task<ActionResult<IEnumerable<EmployeeDocumentTypeResponse>>> GetByAppliesTo(string appliesTo)
    {
        var types = await _service.GetByAppliesToAsync(appliesTo);
        return Ok(types);
    }

    /// <summary>
    /// Obtiene un tipo de documento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDocumentTypeResponse>> GetById(int id)
    {
        var type = await _service.GetByIdAsync(id);
        if (type is null)
            return NotFound(new { message = "Tipo de documento no encontrado" });

        return Ok(type);
    }

    /// <summary>
    /// Crea un nuevo tipo de documento
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeDocumentTypeResponse>> Create([FromBody] EmployeeDocumentTypeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var type = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un tipo de documento
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeDocumentTypeResponse>> Update(int id, [FromBody] EmployeeDocumentTypeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var type = await _service.UpdateAsync(id, request);
            if (type is null)
                return NotFound(new { message = "Tipo de documento no encontrado" });

            return Ok(type);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Activa/Desactiva un tipo de documento
    /// </summary>
    [HttpPatch("{id}/toggle")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _service.ToggleActiveAsync(id);
        if (!result)
            return NotFound(new { message = "Tipo de documento no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Elimina un tipo de documento
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Tipo de documento no encontrado" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
