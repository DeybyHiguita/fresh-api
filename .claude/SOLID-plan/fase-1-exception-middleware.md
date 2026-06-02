# Fase 1 — Middleware de Excepciones Global

**Principio:** Single Responsibility (SRP) en Controllers  
**Esfuerzo estimado:** 2–4 horas  
**Riesgo:** Bajo — no toca lógica de negocio  
**Prioridad:** Alta — elimina código duplicado en todos los controllers  

---

## Problema actual

Cada action de cada controller repite el mismo patrón de manejo de errores:

```csharp
// OrdersController.cs — se repite en CADA action
try
{
    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(...);
}
catch (KeyNotFoundException ex)
{
    return NotFound(new { message = ex.Message });
}
catch (InvalidOperationException ex)
{
    return BadRequest(new { message = ex.Message });
}
```

Este patrón aparece en los 40 controllers. El controller tiene dos responsabilidades: manejar HTTP y manejar errores. Viola SRP.

---

## Solución: ExceptionHandlingMiddleware

Un solo middleware intercepta todas las excepciones no manejadas y las convierte al HTTP status correcto.

### Paso 1 — Crear el middleware

Crear archivo: `Fresh.Api/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
using System.Net;
using System.Text.Json;

namespace Fresh.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            KeyNotFoundException e       => (HttpStatusCode.NotFound, e.Message),
            InvalidOperationException e  => (HttpStatusCode.BadRequest, e.Message),
            UnauthorizedAccessException e => (HttpStatusCode.Unauthorized, e.Message),
            ArgumentException e          => (HttpStatusCode.BadRequest, e.Message),
            _                            => (HttpStatusCode.InternalServerError, "Ocurrió un error inesperado.")
        };

        // Log errores no controlados (500)
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var body = JsonSerializer.Serialize(new { message });
        await context.Response.WriteAsync(body);
    }
}
```

### Paso 2 — Registrar en Program.cs

Agregar **antes** de `app.UseAuthentication()`:

```csharp
// Program.cs
app.UseMiddleware<ExceptionHandlingMiddleware>(); // ← agregar aquí
app.UseAuthentication();
app.UseAuthorization();
```

### Paso 3 — Limpiar los controllers

Con el middleware activo, cada action queda así:

```csharp
// ANTES
[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(OrderRequest request)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);
    try
    {
        var order = await _orderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
    catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
}

// DESPUÉS
[HttpPost]
public async Task<ActionResult<OrderResponse>> Create(OrderRequest request)
{
    if (!ModelState.IsValid) return BadRequest(ModelState);
    var order = await _orderService.CreateAsync(request);
    return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
}
```

Repetir para los 40 controllers eliminando todos los bloques `try/catch`.

---

## Orden de implementación

1. Crear `ExceptionHandlingMiddleware.cs`
2. Registrar en `Program.cs`
3. Probar manualmente que un `KeyNotFoundException` retorna 404
4. Limpiar `OrdersController.cs` primero (el más complejo) y verificar
5. Limpiar el resto de controllers

---

## Criterios de aceptación

- [ ] La API compila sin errores
- [ ] Un request que lanza `KeyNotFoundException` retorna HTTP 404 con `{ "message": "..." }`
- [ ] Un request que lanza `InvalidOperationException` retorna HTTP 400
- [ ] Ningún controller tiene bloques `try/catch`
- [ ] Los errores 500 quedan registrados en el log
