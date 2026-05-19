using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Fresh.Api.Services;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/drive-setup")]
[AllowAnonymous]
public class DriveSetupController : ControllerBase
{
    private readonly GoogleDriveService _driveService;

    public DriveSetupController(GoogleDriveService driveService)
    {
        _driveService = driveService;
    }

    /// <summary>Lists subfolders inside the root Drive folder.</summary>
    [HttpGet("folders")]
    public async Task<IActionResult> GetFolders()
    {
        if (!_driveService.IsAuthorized())
            return Ok(new { folders = Array.Empty<object>() });

        try
        {
            var folders = await _driveService.ListSubfoldersAsync();
            return Ok(new { folders = folders.Select(f => new { f.Id, f.Name }) });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Checks whether Drive is authorized (refresh token exists).</summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { authorized = _driveService.IsAuthorized() });
    }

    /// <summary>Returns the Google OAuth2 consent URL. Optional returnPath is encoded in state.</summary>
    [HttpGet("auth-url")]
    public IActionResult GetAuthUrl([FromQuery] string? returnPath = null)
    {
        try
        {
            var url = _driveService.GetAuthorizationUrl(returnPath);
            return Ok(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Exchanges an OAuth2 code for a refresh token and saves it to disk.</summary>
    [HttpGet("exchange")]
    public async Task<IActionResult> Exchange([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "El parámetro 'code' es requerido." });

        try
        {
            await _driveService.ExchangeCodeForRefreshTokenAsync(code);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al canjear el código.", detail = ex.Message });
        }
    }
}
