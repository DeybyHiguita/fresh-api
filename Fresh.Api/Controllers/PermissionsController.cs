using System.Security.Claims;
using Fresh.Core.DTOs.Permission;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _service;

    public PermissionsController(IPermissionService service)
    {
        _service = service;
    }

    // GET /api/permissions  → todos los usuarios con sus permisos (solo admin)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserPermissionsResponse>>> GetAll()
    {
        if (!IsAdmin()) return Forbid();
        return Ok(await _service.GetAllUsersAsync());
    }

    // GET /api/permissions/me  → permisos del usuario autenticado
    [HttpGet("me")]
    public async Task<ActionResult<UserPermissionsResponse>> GetMe()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        try
        {
            return Ok(await _service.GetByUserIdAsync(userId.Value));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // GET /api/permissions/{userId}  → permisos de un usuario (solo admin)
    [HttpGet("{userId}")]
    public async Task<ActionResult<UserPermissionsResponse>> GetByUser(int userId)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            return Ok(await _service.GetByUserIdAsync(userId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // PUT /api/permissions/{userId}  → actualizar permisos (solo admin)
    [HttpPut("{userId}")]
    public async Task<ActionResult<UserPermissionsResponse>> Update(
        int userId, [FromBody] UpdateUserPermissionsRequest request)
    {
        if (!IsAdmin()) return Forbid();

        try
        {
            return Ok(await _service.UpdateAsync(userId, request));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool IsAdmin() =>
        User.FindFirst(ClaimTypes.Role)?.Value == "admin";

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
