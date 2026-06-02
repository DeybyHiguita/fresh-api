# Fase 4 — Open/Closed en Transiciones de Estado

**Principio:** Open/Closed Principle (OCP)  
**Esfuerzo estimado:** 4–6 horas  
**Riesgo:** Bajo — cambio localizado en `OrderService`  
**Prioridad:** Baja — mejora extensibilidad pero no afecta funcionamiento actual  

---

## Problema actual

`UpdateStatusAsync` en `OrderService` probablemente valida transiciones con una cadena de `if` o `switch`:

```csharp
// Patrón típico — viola OCP
public async Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null)
{
    // ...
    if (order.Status == "Pending" && newStatus == "InProgress") { /* ok */ }
    else if (order.Status == "InProgress" && newStatus == "Ready") { /* ok */ }
    else if (order.Status == "Ready" && newStatus == "Delivered") { /* ok */ }
    else throw new InvalidOperationException("Transición de estado inválida.");
}
```

Para agregar un nuevo estado (por ejemplo `"OnHold"`), hay que **modificar** este método. OCP dice que el código debe estar **abierto para extensión, cerrado para modificación**.

---

## Solución: tabla de transiciones declarativa

### Paso 1 — Definir las transiciones como datos

Nuevo archivo: `Fresh.Core/Domain/OrderStateMachine.cs`

```csharp
namespace Fresh.Core.Domain;

public static class OrderStateMachine
{
    /// <summary>
    /// Define las transiciones válidas: EstadoOrigen → [EstadosDestino permitidos]
    /// Para agregar un nuevo estado, solo se agrega una entrada aquí.
    /// </summary>
    private static readonly Dictionary<string, string[]> _validTransitions = new()
    {
        ["Pending"]    = ["InProgress", "Cancelled"],
        ["InProgress"] = ["Ready", "Cancelled"],
        ["Ready"]      = ["Delivered", "Cancelled"],
        ["Delivered"]  = [],           // estado final
        ["Cancelled"]  = [],           // estado final
    };

    public static bool CanTransition(string from, string to) =>
        _validTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static void ValidateTransition(string from, string to)
    {
        if (!CanTransition(from, to))
            throw new InvalidOperationException(
                $"No se puede cambiar el estado de '{from}' a '{to}'.");
    }

    public static string[] GetAllowedTransitions(string from) =>
        _validTransitions.TryGetValue(from, out var allowed) ? allowed : [];
}
```

### Paso 2 — Usar en OrderService

```csharp
// ANTES
public async Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null)
{
    var order = await _context.Orders.FindAsync(id);
    if (order == null) return null;

    // Lógica de validación manual con if/else
    if (order.Status == "Pending" && newStatus == "InProgress") { }
    else if (...) { }
    else throw new InvalidOperationException("Transición inválida.");

    order.Status = newStatus;
    // ...
}

// DESPUÉS
public async Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null)
{
    var order = await _context.Orders.FindAsync(id);
    if (order == null) return null;

    OrderStateMachine.ValidateTransition(order.Status, newStatus); // ← una línea

    order.Status = newStatus;
    order.Notes = notes;
    order.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();
    return OrderMapper.ToResponse(order);
}
```

### Paso 3 — Agregar constantes de estado (opcional pero recomendado)

Para evitar strings mágicos dispersos en el código:

Nuevo archivo: `Fresh.Core/Domain/OrderStatus.cs`

```csharp
namespace Fresh.Core.Domain;

public static class OrderStatus
{
    public const string Pending    = "Pending";
    public const string InProgress = "InProgress";
    public const string Ready      = "Ready";
    public const string Delivered  = "Delivered";
    public const string Cancelled  = "Cancelled";
}
```

Uso:

```csharp
private static readonly Dictionary<string, string[]> _validTransitions = new()
{
    [OrderStatus.Pending]    = [OrderStatus.InProgress, OrderStatus.Cancelled],
    [OrderStatus.InProgress] = [OrderStatus.Ready, OrderStatus.Cancelled],
    [OrderStatus.Ready]      = [OrderStatus.Delivered, OrderStatus.Cancelled],
    [OrderStatus.Delivered]  = [],
    [OrderStatus.Cancelled]  = [],
};
```

---

## Cómo extender sin modificar

Si en el futuro se agrega un estado `"OnHold"`:

```csharp
// Solo agregar esta línea al diccionario — nada más cambia
[OrderStatus.Pending]    = [OrderStatus.InProgress, OrderStatus.OnHold, OrderStatus.Cancelled],
["OnHold"]               = [OrderStatus.InProgress, OrderStatus.Cancelled],
```

El método `UpdateStatusAsync` no se toca. Eso es OCP.

---

## Aplicar el mismo patrón a otros estados

Si la API maneja estados en otras entidades, aplicar el mismo patrón:

| Entidad | Campo de estado | Acción |
|---|---|---|
| `Invoice` | `Status` | Crear `InvoiceStateMachine` |
| `CashPeriod` | `Status` | Crear `CashPeriodStateMachine` |
| `WorkShift` | `Status` | Crear `WorkShiftStateMachine` |

---

## Orden de implementación

1. Crear `OrderStatus.cs` con las constantes
2. Crear `OrderStateMachine.cs` con el diccionario
3. Reemplazar la lógica de `if/else` en `UpdateStatusAsync`
4. Buscar en todo el proyecto strings como `"Pending"`, `"Delivered"` y reemplazar con las constantes
5. Repetir para otras entidades con estados si aplica

---

## Criterios de aceptación

- [ ] `UpdateStatusAsync` no contiene `if/else` ni `switch` para validar transiciones
- [ ] `OrderStateMachine` define todas las transiciones válidas en un solo lugar
- [ ] Agregar un nuevo estado requiere solo editar el diccionario de `OrderStateMachine`
- [ ] No existen strings mágicos de estado dispersos en el código (usar `OrderStatus.*`)
- [ ] Las transiciones inválidas siguen retornando 400 con mensaje claro
