using Fresh.Core.DTOs.Sidebar;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/sidebar-config")]
[Authorize]
public class SidebarNavConfigController : ControllerBase
{
    private readonly ISidebarNavConfigService _service;

    public SidebarNavConfigController(ISidebarNavConfigService service)
    {
        _service = service;
    }

    /// <summary>Returns the current sidebar config. Returns null when no config has been saved yet (frontend uses default).</summary>
    [HttpGet]
    public async Task<ActionResult<SidebarNavConfigDto?>> Get()
    {
        var config = await _service.GetAsync();
        return Ok(config);
    }

    /// <summary>Saves the sidebar config. Admin-only operation.</summary>
    [HttpPut]
    public async Task<ActionResult<SidebarNavConfigDto>> Save([FromBody] SidebarNavConfigDto config)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var saved = await _service.SaveAsync(config);
        return Ok(saved);
    }
}
