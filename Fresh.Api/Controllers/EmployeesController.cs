using Fresh.Core.DTOs.Employee;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Obtiene todos los empleados
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetAll()
    {
        var employees = await _employeeService.GetAllAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Obtiene empleados activos
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<EmployeeResponse>>> GetActive()
    {
        var employees = await _employeeService.GetActiveAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Obtiene un empleado por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeResponse>> GetById(int id)
    {
        var employee = await _employeeService.GetByIdAsync(id);
        if (employee is null)
            return NotFound(new { message = "Empleado no encontrado" });

        return Ok(employee);
    }

    /// <summary>
    /// Obtiene el empleado del usuario actual
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<EmployeeResponse>> GetMyEmployee()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var employee = await _employeeService.GetByUserIdAsync(userId);
        if (employee is null)
            return NotFound(new { message = "No tienes un perfil de empleado asociado" });

        return Ok(employee);
    }

    /// <summary>
    /// Busca empleado por documento
    /// </summary>
    [HttpGet("by-document/{documentType}/{documentNumber}")]
    public async Task<ActionResult<EmployeeResponse>> GetByDocument(string documentType, string documentNumber)
    {
        var employee = await _employeeService.GetByDocumentAsync(documentType, documentNumber);
        if (employee is null)
            return NotFound(new { message = "Empleado no encontrado" });

        return Ok(employee);
    }

    /// <summary>
    /// Crea un nuevo empleado
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeResponse>> Create([FromBody] EmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var employee = await _employeeService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Inner exception tiene el error real de PostgreSQL
            var innerMsg = dbEx.InnerException?.Message ?? dbEx.Message;
            Console.WriteLine($"[EmployeesController.Create] DbUpdateException: {innerMsg}");
            return BadRequest(new { message = innerMsg });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmployeesController.Create] Exception: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un empleado
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeResponse>> Update(int id, [FromBody] EmployeeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verificar permisos: admin puede editar cualquiera, el usuario solo su propio perfil
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin && !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            var myEmployee = await _employeeService.GetByUserIdAsync(userId);
            if (myEmployee?.Id != id)
                return Forbid();
        }

        try
        {
            var employee = await _employeeService.UpdateAsync(id, request);
            if (employee is null)
                return NotFound(new { message = "Empleado no encontrado" });

            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Vincula un usuario a un empleado
    /// </summary>
    [HttpPost("{id}/link-user")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeResponse>> LinkUser(int id, [FromBody] LinkUserRequest request)
    {
        try
        {
            var employee = await _employeeService.LinkUserAsync(id, request);
            if (employee is null)
                return NotFound(new { message = "Empleado no encontrado" });

            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Desvincula el usuario de un empleado
    /// </summary>
    [HttpPost("{id}/unlink-user")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeResponse>> UnlinkUser(int id)
    {
        var employee = await _employeeService.UnlinkUserAsync(id);
        if (employee is null)
            return NotFound(new { message = "Empleado no encontrado" });

        return Ok(employee);
    }

    /// <summary>
    /// Da de baja a un empleado
    /// </summary>
    [HttpPost("{id}/terminate")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeResponse>> Terminate(int id, [FromBody] TerminateEmployeeRequest request)
    {
        try
        {
            var employee = await _employeeService.TerminateAsync(id, request);
            if (employee is null)
                return NotFound(new { message = "Empleado no encontrado" });

            return Ok(employee);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reactiva un empleado dado de baja
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeResponse>> Reactivate(int id)
    {
        var employee = await _employeeService.ReactivateAsync(id);
        if (employee is null)
            return NotFound(new { message = "Empleado no encontrado" });

        return Ok(employee);
    }

    /// <summary>
    /// Elimina un empleado
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _employeeService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Empleado no encontrado" });

        return NoContent();
    }

    /// <summary>
    /// Genera y descarga el certificado laboral del empleado
    /// </summary>
    [HttpGet("{id}/labor-certificate")]
    public async Task<IActionResult> GetLaborCertificate(int id)
    {
        // Verificar permisos
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin && !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
        {
            var myEmployee = await _employeeService.GetByUserIdAsync(userId);
            if (myEmployee?.Id != id)
                return Forbid();
        }

        try
        {
            var certificate = await _employeeService.GenerateLaborCertificateAsync(id);
            return File(certificate, "text/plain", $"certificado_laboral_{id}.txt");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Genera y descarga el certificado laboral del usuario actual
    /// </summary>
    [HttpGet("me/labor-certificate")]
    public async Task<IActionResult> GetMyLaborCertificate()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var employee = await _employeeService.GetByUserIdAsync(userId);
        if (employee is null)
            return NotFound(new { message = "No tienes un perfil de empleado asociado" });

        try
        {
            var certificate = await _employeeService.GenerateLaborCertificateAsync(employee.Id);
            return File(certificate, "text/plain", $"certificado_laboral.txt");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
