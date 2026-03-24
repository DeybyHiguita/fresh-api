using Fresh.Core.DTOs.UserSession;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/user-sessions")]
[Authorize]
public class UserSessionsController : ControllerBase
{
    private readonly IUserSessionService _service;
    public UserSessionsController(IUserSessionService service) { _service = service; }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<UserSessionResponse>>> GetActive()
        => Ok(await _service.GetActiveSessionsAsync());

    [HttpGet("user/{userId}/history")]
    public async Task<ActionResult<IEnumerable<UserSessionResponse>>> GetHistory(int userId)
        => Ok(await _service.GetHistoryByUserIdAsync(userId));

    [HttpGet("{sessionId}/actions")]
    public async Task<ActionResult<IEnumerable<UserActionResponse>>> GetActions(int sessionId)
        => Ok(await _service.GetSessionActionsAsync(sessionId));
}