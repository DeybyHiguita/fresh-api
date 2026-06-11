using Fresh.Core.DTOs.Store;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _service;
    public StoresController(IStoreService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreResponse>>> GetAll()
        => Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreResponse>> GetById(int id)
    {
        var store = await _service.GetByIdAsync(id);
        return store == null ? NotFound() : Ok(store);
    }

    [HttpPost]
    public async Task<ActionResult<StoreResponse>> Create([FromBody] StoreRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var store = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = store.Id }, store);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StoreResponse>> Update(int id, [FromBody] StoreRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var store = await _service.UpdateAsync(id, request);
        return store == null ? NotFound() : Ok(store);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id}/users")]
    public async Task<ActionResult<IEnumerable<StoreUserResponse>>> GetUsers(int id)
        => Ok(await _service.GetStoreUsersAsync(id));

    [HttpPost("{id}/users/{userId}")]
    public async Task<IActionResult> AddUser(int id, int userId, [FromQuery] bool isDefault = false)
    {
        await _service.AddUserToStoreAsync(id, userId, isDefault);
        return NoContent();
    }

    [HttpDelete("{id}/users/{userId}")]
    public async Task<IActionResult> RemoveUser(int id, int userId)
    {
        await _service.RemoveUserFromStoreAsync(id, userId);
        return NoContent();
    }
}
