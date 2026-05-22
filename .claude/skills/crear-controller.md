# Skill: Crear Controller

## Descripción

Guía para crear un nuevo controlador API REST siguiendo el patrón del proyecto Fresh.

## Plantilla Base

```csharp
// Fresh.Api/Controllers/{EntidadPlural}Controller.cs
using Fresh.Core.DTOs.{Entidad};
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class {EntidadPlural}Controller : ControllerBase
{
    private readonly I{Entidad}Service _{entidad}Service;

    public {EntidadPlural}Controller(I{Entidad}Service {entidad}Service)
    {
        _{entidad}Service = {entidad}Service;
    }

    /// <summary>
    /// Obtiene todos los registros
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<{Entidad}Response>>> GetAll()
    {
        var items = await _{entidad}Service.GetAllAsync();
        return Ok(items);
    }

    /// <summary>
    /// Obtiene un registro por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<{Entidad}Response>> GetById(int id)
    {
        var item = await _{entidad}Service.GetByIdAsync(id);
        if (item is null)
            return NotFound(new { message = "{Entidad} no encontrado" });

        return Ok(item);
    }

    /// <summary>
    /// Crea un nuevo registro
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<{Entidad}Response>> Create([FromBody] {Entidad}Request request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var item = await _{entidad}Service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza un registro existente
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<{Entidad}Response>> Update(int id, [FromBody] {Entidad}Request request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var item = await _{entidad}Service.UpdateAsync(id, request);
            if (item is null)
                return NotFound(new { message = "{Entidad} no encontrado" });

            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un registro
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var deleted = await _{entidad}Service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "{Entidad} no encontrado" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
```

## Variaciones Comunes

### Endpoint con Filtros

```csharp
[HttpGet("by-month/{year}/{month}")]
public async Task<ActionResult<IEnumerable<ExpenseResponse>>> GetByMonth(int year, int month)
{
    var items = await _expenseService.GetByMonthAsync(year, month);
    return Ok(items);
}
```

### Endpoint con Query Parameters

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll(
    [FromQuery] bool? isPending = null,
    [FromQuery] DateTime? from = null,
    [FromQuery] DateTime? to = null)
{
    var items = await _orderService.GetFilteredAsync(isPending, from, to);
    return Ok(items);
}
```

### Sin Autorización (Público)

```csharp
[HttpGet("menu")]
[AllowAnonymous]
public async Task<ActionResult<IEnumerable<MenuItemResponse>>> GetPublicMenu()
{
    var items = await _menuService.GetActiveMenuAsync();
    return Ok(items);
}
```

### Con Rol Específico

```csharp
[HttpDelete("{id}")]
[Authorize(Roles = "admin")]
public async Task<IActionResult> Delete(int id)
{
    // Solo admin puede eliminar
}
```

### Subir Archivo

```csharp
[HttpPost("{id}/image")]
public async Task<IActionResult> UploadImage(int id, IFormFile file)
{
    if (file is null || file.Length == 0)
        return BadRequest(new { message = "Archivo inválido" });

    var url = await _service.SaveImageAsync(id, file);
    return Ok(new { imageUrl = url });
}
```

## Respuestas HTTP Estándar

| Método | Éxito | Error |
|--------|-------|-------|
| GET (lista) | 200 OK | - |
| GET (uno) | 200 OK | 404 NotFound |
| POST | 201 Created | 400 BadRequest, 409 Conflict |
| PUT | 200 OK | 404 NotFound, 409 Conflict |
| DELETE | 204 NoContent | 404 NotFound, 409 Conflict |

## Lista de Verificación

- [ ] Controller hereda de `ControllerBase`
- [ ] Atributos `[ApiController]`, `[Route]`, `[Authorize]`
- [ ] Servicio inyectado vía constructor
- [ ] Métodos async
- [ ] Validación de `ModelState` en POST/PUT
- [ ] Respuestas HTTP apropiadas
- [ ] Mensajes de error descriptivos en español
