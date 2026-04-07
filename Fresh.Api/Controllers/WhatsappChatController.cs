using Fresh.Core.DTOs.WhatsappChat;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/whatsapp/chat")]
[Authorize(Roles = "admin")]
public class WhatsappChatController : ControllerBase
{
    private readonly WhatsappChatService _chat;

    public WhatsappChatController(WhatsappChatService chat)
    {
        _chat = chat;
    }

    /// <summary>Lista de contactos ordenados por último mensaje.</summary>
    [HttpGet("contacts")]
    public async Task<ActionResult<List<WhatsappContactDto>>> GetContacts()
        => Ok(await _chat.GetContactsAsync());

    /// <summary>Mensajes de un contacto. Marca mensajes como leídos.</summary>
    [HttpGet("contacts/{contactId:int}/messages")]
    public async Task<ActionResult<List<WhatsappMessageDto>>> GetMessages(int contactId)
        => Ok(await _chat.GetMessagesAsync(contactId));

    /// <summary>Envía un mensaje de texto al contacto.</summary>
    [HttpPost("send")]
    public async Task<ActionResult<WhatsappMessageDto>> Send([FromBody] SendMessageRequest req)
    {
        try
        {
            var msg = await _chat.SendReplyAsync(req);
            return Ok(msg);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>Envía una imagen, documento o audio al contacto.</summary>
    [HttpPost("send-media")]
    [RequestSizeLimit(25 * 1024 * 1024)] // 25 MB (límite de Meta)
    public async Task<ActionResult<WhatsappMessageDto>> SendMedia(
        [FromForm] int contactId,
        [FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Archivo vacío." });

        try
        {
            var msg = await _chat.SendMediaAsync(contactId, file);
            return Ok(msg);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>
    /// Proxy para descargar desde Meta sin exponer el token al frontend.
    /// GET /api/whatsapp/chat/media/{mediaId}
    /// </summary>
    [HttpGet("media/{mediaId}")]
    public async Task<IActionResult> GetMedia(string mediaId)
    {
        var (data, mimeType) = await _chat.DownloadMediaAsync(mediaId);
        if (data is null) return NotFound();
        return File(data, mimeType ?? "application/octet-stream");
    }
}
