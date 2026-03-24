using Fresh.Core.DTOs.AppPage;
using Fresh.Core.DTOs.UserPermission;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/user-permissions")]
[Authorize]
public class UserPermissionsController : ControllerBase
{
    private readonly IUserPermissionService _service;
    public UserPermissionsController(IUserPermissionService service) { _service = service; }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<UserPermissionResponse>>> GetUserPermissions(int userId)
        => Ok(await _service.GetByUserIdAsync(userId));

    [HttpGet("user/{userId}/menu")]
    public async Task<ActionResult<IEnumerable<AppPageResponse>>> GetUserMenu(int userId)
        => Ok(await _service.GetMenuForUserAsync(userId));

    [HttpPut("user/{userId}")]
    public async Task<ActionResult<IEnumerable<UserPermissionResponse>>> UpdatePermissions(int userId, [FromBody] IEnumerable<UserPermissionRequest> requests)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var result = await _service.UpdateUserPermissionsAsync(userId, requests);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
}