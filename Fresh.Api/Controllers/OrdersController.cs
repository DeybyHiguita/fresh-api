using Fresh.Api.Hubs;
using Fresh.Core.DTOs.Order;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IHubContext<OrderHub> _orderHub;
    private readonly WhatsAppNotificationService _whatsApp;

    public OrdersController(
        IOrderService orderService,
        IHubContext<OrderHub> orderHub,
        WhatsAppNotificationService whatsApp)
    {
        _orderService = orderService;
        _orderHub     = orderHub;
        _whatsApp     = whatsApp;
    }

    private int StoreId => int.TryParse(User.FindFirst("store_id")?.Value, out var id) ? id : 0;

    /// <summary>
    /// Notifica a los admins de la tienda de la orden y a los superadmins en vista global.
    /// </summary>
    private Task NotifyAdminsAsync(string method, OrderResponse order)
    {
        var storeGroup = OrderHub.StoreAdminsGroup(order.StoreId);
        var groups = storeGroup == "store:all:admins"
            ? new[] { "store:all:admins" }
            : new[] { storeGroup, "store:all:admins" };
        return _orderHub.Clients.Groups(groups).SendAsync(method, order);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        return Ok(await _orderService.GetAllAsync(StoreId));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null) return NotFound();
        return Ok(order);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create([FromBody] OrderRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var order = await _orderService.CreateAsync(request, StoreId);

            // Notificar a los administradores de esta tienda (+ superadmins en vista global)
            await NotifyAdminsAsync("NewOrder", order);

            // WhatsApp
            await _whatsApp.NotifyNewOrderAsync(order);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // Endpoint de cierre de estado, como indica tu guía con HttpPatch
    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
            return BadRequest(new { message = "El estado no puede estar vacío" });

        try
        {
            var order = await _orderService.UpdateStatusAsync(id, request.Status, request.Notes);
            await NotifyAdminsAsync("OrderUpdated", order);

            // WhatsApp
            await _whatsApp.NotifyStatusChangedAsync(order);

            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/payment-method")]
    public async Task<ActionResult<OrderResponse>> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return BadRequest(new { message = "El medio de pago no puede estar vacío" });

        try
        {
            var order = await _orderService.UpdatePaymentMethodAsync(id, request.PaymentMethod, request.CustomerId);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "admin")]
    [HttpPatch("{id}/items")]
    public async Task<ActionResult<OrderResponse>> UpdateItems(int id, [FromBody] UpdateOrderItemsRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var order = await _orderService.UpdateItemsAsync(id, request.Items);
            if (order == null) return NotFound();
            await NotifyAdminsAsync("OrderUpdated", order);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}