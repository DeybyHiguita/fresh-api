using Fresh.Core.DTOs.EmployeeDocument;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/employees/{employeeId}/documents")]
[Authorize]
public class EmployeeDocumentsController : ControllerBase
{
    private readonly IEmployeeDocumentService _service;
    private readonly IEmployeeService _employeeService;

    public EmployeeDocumentsController(
        IEmployeeDocumentService service,
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
    /// Obtiene documentos de un empleado
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmployeeDocumentResponse>>> GetByEmployee(int employeeId)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var documents = await _service.GetByEmployeeAsync(employeeId);
        return Ok(documents);
    }

    /// <summary>
    /// Obtiene un documento por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDocumentResponse>> GetById(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var document = await _service.GetByIdAsync(id);
        if (document is null || document.EmployeeId != employeeId)
            return NotFound(new { message = "Documento no encontrado" });

        return Ok(document);
    }

    /// <summary>
    /// Sube un nuevo documento
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<EmployeeDocumentResponse>> Upload(
        int employeeId,
        IFormFile file,
        [FromForm] int documentTypeId,
        [FromForm] string? notes,
        [FromForm] DateTime? expirationDate)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Archivo no válido" });

        var request = new EmployeeDocumentRequest
        {
            DocumentTypeId = documentTypeId,
            Notes = notes,
            ExpirationDate = expirationDate
        };

        try
        {
            var document = await _service.UploadAsync(employeeId, file, request, GetCurrentUserId());
            return CreatedAtAction(nameof(GetById), new { employeeId, id = document.Id }, document);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza metadatos de un documento
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmployeeDocumentResponse>> Update(
        int employeeId,
        int id,
        [FromBody] EmployeeDocumentRequest request)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var document = await _service.UpdateAsync(id, request);
        if (document is null)
            return NotFound(new { message = "Documento no encontrado" });

        return Ok(document);
    }

    /// <summary>
    /// Marca un documento como verificado (solo admin)
    /// </summary>
    [HttpPost("{id}/verify")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<EmployeeDocumentResponse>> Verify(int employeeId, int id)
    {
        var document = await _service.VerifyAsync(id, GetCurrentUserId());
        if (document is null)
            return NotFound(new { message = "Documento no encontrado" });

        return Ok(document);
    }

    /// <summary>
    /// Descarga un documento
    /// </summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        try
        {
            var (content, fileName, contentType) = await _service.DownloadAsync(id);
            return File(content, contentType, fileName);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un documento
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int employeeId, int id)
    {
        if (!await CanAccessEmployeeAsync(employeeId))
            return Forbid();

        var deleted = await _service.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = "Documento no encontrado" });

        return NoContent();
    }
}
