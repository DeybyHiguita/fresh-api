using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

/// <summary>
/// Hub para notificaciones del chat WhatsApp en tiempo real.
/// Todos los usuarios autenticados se unen al grupo "whatsapp_chat".
/// El frontend filtra por permiso de página antes de procesar los mensajes.
/// </summary>
[Authorize]
public class WhatsappHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Todos los autenticados van al grupo: el frontend decide si procesa
        await Groups.AddToGroupAsync(Context.ConnectionId, "whatsapp_chat");
        await base.OnConnectedAsync();
    }
}
