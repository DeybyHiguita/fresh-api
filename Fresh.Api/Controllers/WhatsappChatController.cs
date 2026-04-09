using Fresh.Core.DTOs.WhatsappChat;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/whatsapp/chat")]
[Authorize]
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
    {
        try { return Ok(await _chat.GetMessagesAsync(contactId)); }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

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
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error interno: {ex.Message}" });
        }
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

    /// <summary>Envía prompt interactivo con botón para solicitar datos de domicilio.</summary>
    [HttpPost("send-delivery-prompt")]
    public async Task<ActionResult<WhatsappMessageDto>> SendDeliveryPrompt([FromBody] DeliveryPromptRequest req)
    {
        try
        {
            var msg = await _chat.SendDeliveryPromptAsync(req.ContactId);
            return Ok(msg);
        }
        catch (KeyNotFoundException ex)      { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        catch (Exception ex)                 { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Envía el menú de bienvenida interactivo (lista con 3 opciones) al contacto.</summary>
    [HttpPost("send-welcome-menu")]
    public async Task<IActionResult> SendWelcomeMenu([FromBody] DeliveryPromptRequest req)
    {
        try
        {
            var contact = await _chat.GetContactWaIdAsync(req.ContactId);
            await _chat.SendWelcomeMenuAsync(contact);
            return Ok();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)            { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Marca los mensajes entrantes del contacto como leídos en WhatsApp (doble tick azul).</summary>
    [HttpPost("contacts/{contactId:int}/mark-read")]
    public async Task<IActionResult> MarkRead(int contactId)
    {
        try
        {
            await _chat.MarkContactMessagesReadAsync(contactId);
            return Ok();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (Exception ex)            { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>
    /// Proxy para descargar desde Meta sin exponer el token al frontend.
    /// GET /api/whatsapp/chat/media/{mediaId}
    /// </summary>
    [AllowAnonymous]
    [HttpGet("media/{mediaId}")]
    public async Task<IActionResult> GetMedia(string mediaId)
    {
        var (data, mimeType) = await _chat.DownloadMediaAsync(mediaId);
        if (data is null) return NotFound();
        return File(data, mimeType ?? "application/octet-stream");
    }
}
