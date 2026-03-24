using Fresh.Core.DTOs.User;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;
    public UsersController(IUserService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponse>> GetById(int id)
    {
        var user = await _service.GetByIdAsync(id);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(int id, [FromBody] UserUpdateRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _service.UpdateAsync(id, request);
        return user == null ? NotFound() : Ok(user);
    }
}
