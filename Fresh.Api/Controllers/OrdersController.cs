using Fresh.Api.Hubs;
using Fresh.Core.DTOs.Order;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IHubContext<OrderHub> _orderHub;

    public OrdersController(IOrderService orderService, IHubContext<OrderHub> orderHub)
    {
        _orderService = orderService;
        _orderHub    = orderHub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        return Ok(await _orderService.GetAllAsync());
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
            var order = await _orderService.CreateAsync(request);

            // Notificar a todos los administradores conectados
            await _orderHub.Clients.Group("admins").SendAsync("NewOrder", order);

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
            await _orderHub.Clients.Group("admins").SendAsync("OrderUpdated", order);
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

    [Authorize]
    [HttpPatch("{id}/payment-method")]
    public async Task<ActionResult<OrderResponse>> UpdatePaymentMethod(int id, [FromBody] UpdatePaymentMethodRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
            return BadRequest(new { message = "El medio de pago no puede estar vacío" });

        try
        {
            var order = await _orderService.UpdatePaymentMethodAsync(id, request.PaymentMethod);
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