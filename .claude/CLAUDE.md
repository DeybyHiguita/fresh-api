# Fresh API - Backend .NET

## Descripción del Proyecto

Fresh es una API REST construida con **.NET 8** siguiendo el patrón **Clean Architecture**. Es el backend de un sistema de gestión empresarial para restaurantes/negocios que incluye: inventario, pedidos, facturación, empleados, WhatsApp Business, caja, inversiones y más.

## Stack Tecnológico

- **Framework**: .NET 8 / ASP.NET Core
- **Base de Datos**: PostgreSQL con Entity Framework Core
- **Autenticación**: JWT Bearer
- **Real-time**: SignalR (Hubs)
- **HTTP Client**: IHttpClientFactory
- **Arquitectura**: Clean Architecture (3 capas)

## Estructura del Proyecto

```
Fresh.Api/           → Capa de presentación (Controllers, Hubs, Middleware)
Fresh.Core/          → Capa de dominio (Entities, DTOs, Interfaces)
Fresh.Infrastructure/→ Capa de datos (DbContext, Services, Repositories)
```

## Convenciones de Código

### Nombres

- **Controladores**: `{Entidad}Controller.cs` con ruta `api/[controller]`
- **Servicios**: `{Entidad}Service.cs` implementa `I{Entidad}Service`
- **Entidades**: Singular en PascalCase (`Order`, `Ingredient`)
- **DTOs**: `{Entidad}Request.cs`, `{Entidad}Response.cs` en carpeta `DTOs/{Entidad}/`
- **Interfaces**: Prefijo `I` (`IOrderService`)

### Patrones

- Inyección de dependencias vía constructor
- Servicios Scoped registrados en `Program.cs`
- Async/await en todas las operaciones de BD
- Mapeo manual Entity ↔ DTO (sin AutoMapper)

### Ejemplo de Controlador

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }
}
```

### Ejemplo de Servicio

```csharp
public class OrderService : IOrderService
{
    private readonly FreshDbContext _context;

    public OrderService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderResponse>> GetAllAsync()
    {
        var orders = await _context.Orders.ToListAsync();
        return orders.Select(MapToResponse);
    }

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        // ... mappings
    };
}
```

## Comandos Comunes

```bash
# Ejecutar en desarrollo
dotnet run --project Fresh.Api

# Build
dotnet build Fresh.sln

# Publicar
dotnet publish Fresh.Api -c Release -o ./publish
```

## Flujo para Nueva Funcionalidad

1. **Entidad** → `Fresh.Core/Entities/{Entidad}.cs`
2. **DTOs** → `Fresh.Core/DTOs/{Entidad}/` (Request + Response)
3. **Interfaz** → `Fresh.Core/Interfaces/I{Entidad}Service.cs`
4. **Servicio** → `Fresh.Infrastructure/Services/{Entidad}Service.cs`
5. **DbSet** → Agregar en `FreshDbContext.cs`
6. **DI** → Registrar en `Program.cs`
7. **Controller** → `Fresh.Api/Controllers/{Entidad}Controller.cs`

## Reglas Importantes

- NO usar AutoMapper, mapear manualmente
- SIEMPRE usar async/await para operaciones de BD
- Los DTOs Request NO incluyen `Id`
- Los DTOs Response SÍ incluyen `Id`, `CreatedAt`, `UpdatedAt`
- Validar `ModelState` en el controlador antes de procesar
- Retornar `NotFound` con mensaje claro cuando no existe el recurso
- Usar `CreatedAtAction` para respuestas de POST exitosas
- Los timestamps usan `DateTime.UtcNow`
- Las entidades incluyen `CreatedAt` y `UpdatedAt` de auditoría

## SignalR Hubs

Los hubs están en `Fresh.Api/Hubs/` y se usan para:
- Notificaciones en tiempo real
- Actualizaciones de WhatsApp
- Estado de órdenes

## Estructura de Respuesta de Errores

```json
{
  "message": "Descripción del error"
}
```

---

Para más detalles, consultar `guia-nueva-caracteristica.md` en la raíz del proyecto.
