using Fresh.Core.DTOs.WhatsappChat;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
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
        catch (KeyNotFoundException ex)   { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
