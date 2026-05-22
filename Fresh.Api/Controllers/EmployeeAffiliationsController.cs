using Fresh.Core.DTOs.EmployeeAffiliation;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/affiliations")]
[Authorize]
public class EmployeeAffiliationsController : ControllerBase
{
    private readonly IEmployeeAffiliationService _service;
    private readonly IEmployeeService _employeeService;

    public EmployeeAffiliationsController(
        IEmployeeAffiliationService service,
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
    /// Obtiene afiliaciones de un empleado
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeAffiliationResponse>>> GetByEmployee(int employeeId)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var affiliations = await _service.GetByEmployeeAsync(employeeId);
        return Ok(affiliations);
    }

    /// <summary>
    /// Obtiene una afiliación por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeAffiliationResponse>> GetById(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var affiliation = await _service.GetByIdAsync(id);
        if (affiliation is null || affiliation.EmployeeId != employeeId)
            return NotFound(new { message = "Afiliación no encontrada" });

        return Ok(affiliation);
    }

    /// <summary>
    /// Obtiene una afiliación por tipo
    /// </summary>
    [HttpGet("by-type/{affiliationType}")]
    public async Task<ActionResult<EmployeeAffiliationResponse>> GetByType(int employeeId, string affiliationType)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var affiliation = await _service.GetByTypeAsync(employeeId, affiliationType);
        if (affiliation is null)
            return NotFound(new { message = "Afiliación no encontrada" });

        return Ok(affiliation);
    }

    /// <summary>
    /// Crea o actualiza una afiliación
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeAffiliationResponse>> CreateOrUpdate(int employeeId, [FromBody] EmployeeAffiliationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var affiliation = await _service.CreateOrUpdateAsync(employeeId, request);
            return Ok(affiliation);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summaryxx    
    /// Elimina una afiliación
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int employeeId, int id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Afiliación no encontrada" });

        return NoContent();
    }
}
