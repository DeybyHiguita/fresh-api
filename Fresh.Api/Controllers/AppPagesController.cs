using Fresh.Core.DTOs.AppPage;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/app-pages")]
[Authorize]
public class AppPagesController : ControllerBase
{
    private readonly IAppPageService _service;
    public AppPagesController(IAppPageService service) { _service = service; }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AppPageResponse>>> GetAll([FromQuery] bool onlyActive = true)
        => Ok(await _service.GetAllAsync(onlyActive));

    [HttpGet("{id}")]
    public async Task<ActionResult<AppPageResponse>> GetById(int id)
    {
        var page = await _service.GetByIdAsync(id);
        return page == null ? NotFound() : Ok(page);
    }

    [HttpPost]
    public async Task<ActionResult<AppPageResponse>> Create([FromBody] AppPageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var page = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = page.Id }, page);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AppPageResponse>> Update(int id, [FromBody] AppPageRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var page = await _service.UpdateAsync(id, request);
            return page == null ? NotFound() : Ok(page);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}