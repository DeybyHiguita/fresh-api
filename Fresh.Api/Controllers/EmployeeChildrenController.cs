using Fresh.Core.DTOs.EmployeeChild;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/children")]
[Authorize]
public class EmployeeChildrenController : ControllerBase
{
    private readonly IEmployeeChildService _service;
    private readonly IEmployeeService _employeeService;

    public EmployeeChildrenController(
        IEmployeeChildService service,
        IEmployeeService employeeService)
    {
        _service = service;
        _employeeService = employeeService;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    private async Task<bool> CanAccessEmployeeAsync(int employeeId)
    {
        if (User.IsInRole("admin")) return true;

        var userId = GetCurrentUserId();
        var employee = await _employeeService.GetByUserIdAsync(userId);
        return employee?.Id == employeeId;
    }

    /// <summary>
    /// Obtiene hijos de un empleado
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeChildResponse>>> GetByEmployee(int employeeId)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var children = await _service.GetByEmployeeAsync(employeeId);
        return Ok(children);
    }

    /// <summary>
    /// Obtiene un hijo por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeChildResponse>> GetById(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var child = await _service.GetByIdAsync(id);
        if (child is null || child.EmployeeId != employeeId)
            return NotFound(new { message = "Hijo no encontrado" });

        return Ok(child);
    }

    /// <summary>
    /// Agrega un hijo a un empleado
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeChildResponse>> Create(int employeeId, [FromBody] EmployeeChildRequest request)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var child = await _service.CreateAsync(employeeId, request);
            return CreatedAtAction(nameof(GetById), new { employeeId, id = child.Id }, child);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza información de un hijo
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeChildResponse>> Update(int employeeId, int id, [FromBody] EmployeeChildRequest request)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var child = await _service.UpdateAsync(id, request);
        if (child is null)
            return NotFound(new { message = "Hijo no encontrado" });

        return Ok(child);
    }

    /// <summary>
    /// Activa/Desactiva un hijo
    /// </summary>
    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleActive(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var result = await _service.ToggleActiveAsync(id);
        if (!result)
            return NotFound(new { message = "Hijo no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Elimina un hijo
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Hijo no encontrado" });

        return NoContent();
    }
}
