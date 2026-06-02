# Fase 2 — Single Responsibility en Servicios

**Principio:** Single Responsibility Principle (SRP)  
**Esfuerzo estimado:** 1–2 días  
**Riesgo:** Medio — toca lógica de negocio, requiere pruebas  
**Prioridad:** Alta — `OrderService` es el caso más crítico  

---

## Problema actual

`OrderService.cs` tiene 432 líneas y mezcla responsabilidades distintas:

| Responsabilidad | Debería estar en |
|---|---|
| Validar que el usuario exista | `OrderValidator` |
| Validar que los menu items existan | `OrderValidator` |
| Calcular subtotal, recargo, descuento | `OrderCalculator` o método en `Order` |
| Validar crédito del cliente | `CustomerCreditService` (ya existe) |
| Orquestar la creación de la orden | `OrderService` ← lo único que debe quedar |
| Crear la factura asociada | `InvoiceService` (ya existe) |
| Mapear `Order` → `OrderResponse` | Clase estática `OrderMapper` |

Un servicio bien definido bajo SRP debería poder describirse en una sola oración. `OrderService` actualmente no puede.

---

## Solución: dividir en clases enfocadas

### Paso 1 — Crear OrderValidator

Nuevo archivo: `Fresh.Core/Validators/OrderValidator.cs`

```csharp
namespace Fresh.Core.Validators;

public class OrderValidator
{
    private readonly FreshDbContext _context;  // temporal hasta Fase 3

    public OrderValidator(FreshDbContext context)
    {
        _context = context;
    }

    public async Task ValidateCreateAsync(OrderRequest request)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);
        if (!userExists)
            throw new KeyNotFoundException($"El usuario con ID {request.UserId} no existe.");

        var menuItemIds = request.Items.Select(i => i.MenuItemId).ToList();
        var validCount = await _context.MenuItems.CountAsync(m => menuItemIds.Contains(m.Id));
        if (validCount != menuItemIds.Distinct().Count())
            throw new InvalidOperationException("Uno o más productos no existen en el menú.");
    }
}
```

Registrar en `Program.cs`:
```csharp
builder.Services.AddScoped<OrderValidator>();
```

### Paso 2 — Crear OrderCalculator

Nuevo archivo: `Fresh.Core/Calculators/OrderCalculator.cs`

```csharp
namespace Fresh.Core.Calculators;

public static class OrderCalculator
{
    public static (decimal Subtotal, decimal Surcharge, decimal Total) Calculate(OrderRequest request)
    {
        decimal subtotal = request.Items.Sum(i => i.Quantity * i.UnitPrice);
        decimal surcharge = request.OrderType == "Delivery" ? request.DeliverySurcharge : 0m;
        decimal total = subtotal + surcharge - request.Discount;

        if (total < 0)
            throw new InvalidOperationException("El descuento no puede ser mayor al subtotal.");

        return (subtotal, surcharge, total);
    }
}
```

Esta clase es estática porque no tiene estado ni dependencias externas — puro cálculo.

### Paso 3 — Crear OrderMapper

Nuevo archivo: `Fresh.Infrastructure/Mappers/OrderMapper.cs`

```csharp
namespace Fresh.Infrastructure.Mappers;

public static class OrderMapper
{
    public static OrderResponse ToResponse(Order order) => new()
    {
        Id = order.Id,
        Status = order.Status,
        OrderType = order.OrderType,
        Total = order.Total,
        // ... resto de campos
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
    };
}
```

Actualmente el método `MapToResponse` vive dentro de `OrderService` como método privado. Extraerlo permite reutilizarlo y testearlo independientemente.

### Paso 4 — OrderService como orquestador puro

Después del refactor, `OrderService.CreateAsync` queda así:

```csharp
public async Task<OrderResponse> CreateAsync(OrderRequest request)
{
    await _validator.ValidateCreateAsync(request);

    var (subtotal, surcharge, total) = OrderCalculator.Calculate(request);

    await using var transaction = await _context.Database.BeginTransactionAsync();

    var order = new Order
    {
        UserId    = request.UserId,
        CustomerId = request.CustomerId,
        Subtotal  = subtotal,
        Surcharge = surcharge,
        Total     = total,
        // ...
    };

    _context.Orders.Add(order);
    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    return OrderMapper.ToResponse(order);
}
```

El servicio orquesta — no valida, no calcula, no mapea.

---

## Patrón a replicar en otros servicios grandes

Después de refactorizar `OrderService`, aplicar el mismo patrón a:

| Servicio | Problema | Extracción sugerida |
|---|---|---|
| `InvoiceService` | Cálculos de totales e impuestos mezclados | `InvoiceCalculator` |
| `CashPeriodService` | Lógica de cierre de caja compleja | `CashPeriodValidator` |
| `WorkShiftService` | Validación de turnos superpuestos | `WorkShiftValidator` |

---

## Orden de implementación

1. Extraer `OrderMapper` (sin riesgo — solo mover código)
2. Crear `OrderCalculator` con sus tests
3. Crear `OrderValidator`
4. Refactorizar `OrderService.CreateAsync` usando las nuevas clases
5. Verificar que la API sigue compilando y respondiendo correctamente
6. Repetir para los otros servicios identificados

---

## Criterios de aceptación

- [ ] `OrderService` tiene menos de 200 líneas
- [ ] `OrderService` no contiene lógica de validación directa
- [ ] `OrderService` no contiene cálculos aritméticos de totales
- [ ] `OrderMapper.ToResponse` es testeable de forma aislada
- [ ] `OrderCalculator.Calculate` es testeable sin base de datos
- [ ] La API compila y los endpoints de órdenes responden correctamente
