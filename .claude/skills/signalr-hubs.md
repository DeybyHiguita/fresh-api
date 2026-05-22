# Skill: SignalR Hubs

## Descripción

Guía para implementar comunicación en tiempo real con SignalR en el proyecto Fresh.

## Estructura

```
Fresh.Api/
├── Hubs/
│   └── {Nombre}Hub.cs        → Hub principal
├── Services/
│   └── {Nombre}HubNotifier.cs → Servicio para enviar notificaciones
└── Fresh.Core/Interfaces/
    └── I{Nombre}HubNotifier.cs → Interfaz del notifier
```

## 1. Crear la Interfaz del Notifier

```csharp
// Fresh.Core/Interfaces/I{Nombre}HubNotifier.cs
namespace Fresh.Core.Interfaces;

public interface IOrderHubNotifier
{
    Task NotifyNewOrderAsync(object orderData);
    Task NotifyOrderUpdatedAsync(int orderId, string status);
    Task NotifyOrderCancelledAsync(int orderId);
}
```

## 2. Crear el Hub

```csharp
// Fresh.Api/Hubs/OrderHub.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Agregar a grupo global o por rol
        await Groups.AddToGroupAsync(Context.ConnectionId, "orders");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "orders");
        await base.OnDisconnectedAsync(exception);
    }

    // Métodos que el cliente puede invocar
    public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }

    public async Task LeaveOrderGroup(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderId}");
    }
}
```

## 3. Crear el Notifier Service

```csharp
// Fresh.Api/Services/OrderHubNotifier.cs
using Fresh.Api.Hubs;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Fresh.Api.Services;

public class OrderHubNotifier : IOrderHubNotifier
{
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderHubNotifier(IHubContext<OrderHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyNewOrderAsync(object orderData)
    {
        await _hubContext.Clients.Group("orders")
            .SendAsync("NewOrder", orderData);
    }

    public async Task NotifyOrderUpdatedAsync(int orderId, string status)
    {
        await _hubContext.Clients.Group("orders")
            .SendAsync("OrderUpdated", new { orderId, status });
    }

    public async Task NotifyOrderCancelledAsync(int orderId)
    {
        await _hubContext.Clients.Group($"order-{orderId}")
            .SendAsync("OrderCancelled", orderId);
    }
}
```

## 4. Registrar en Program.cs

```csharp
// Program.cs

// Servicios
builder.Services.AddSignalR();
builder.Services.AddScoped<IOrderHubNotifier, OrderHubNotifier>();

// JWT para SignalR (ya configurado en el proyecto)
options.Events = new JwtBearerEvents
{
    OnMessageReceived = context =>
    {
        var accessToken = context.Request.Query["access_token"];
        var path = context.HttpContext.Request.Path;
        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
        {
            context.Token = accessToken;
        }
        return Task.CompletedTask;
    }
};

// Mapear Hub
app.MapHub<OrderHub>("/hubs/orders");
```

## 5. Usar en Servicios

```csharp
// Fresh.Infrastructure/Services/OrderService.cs
public class OrderService : IOrderService
{
    private readonly FreshDbContext _context;
    private readonly IOrderHubNotifier _notifier;

    public OrderService(FreshDbContext context, IOrderHubNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<OrderResponse> CreateAsync(OrderRequest request)
    {
        var order = new Order { /* ... */ };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var response = MapToResponse(order);
        
        // Notificar a clientes conectados
        await _notifier.NotifyNewOrderAsync(response);

        return response;
    }
}
```

## Patrones de Notificación

### Broadcast a Todos

```csharp
await _hubContext.Clients.All.SendAsync("EventName", data);
```

### A un Grupo Específico

```csharp
await _hubContext.Clients.Group("kitchen").SendAsync("NewOrder", order);
```

### A un Usuario Específico

```csharp
await _hubContext.Clients.User(userId).SendAsync("Notification", message);
```

### Excepto el Emisor

```csharp
await _hubContext.Clients.GroupExcept("orders", connectionId)
    .SendAsync("OrderUpdated", data);
```

## Cliente Angular

```typescript
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

private hubConnection: HubConnection;

initHub(): void {
  this.hubConnection = new HubConnectionBuilder()
    .withUrl(`${environment.apiUrl}/hubs/orders`, {
      accessTokenFactory: () => this.authService.getToken() ?? '',
    })
    .withAutomaticReconnect()
    .build();

  this.hubConnection.on('NewOrder', (order) => {
    console.log('Nueva orden recibida:', order);
    // Actualizar UI
  });

  this.hubConnection.start()
    .catch(err => console.error('Error conectando al hub:', err));
}

// Invocar método del servidor
joinOrderGroup(orderId: number): void {
  this.hubConnection.invoke('JoinOrderGroup', orderId);
}
```

## Lista de Verificación

- [ ] Interfaz del Notifier en Fresh.Core/Interfaces
- [ ] Hub creado en Fresh.Api/Hubs
- [ ] Notifier implementado en Fresh.Api/Services
- [ ] SignalR registrado con `AddSignalR()`
- [ ] Hub mapeado con `MapHub<>()`
- [ ] JWT configurado para SignalR
- [ ] Notifier registrado como Scoped
