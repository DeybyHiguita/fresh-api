# Skill: Crear Servicio

## Descripción

Guía para crear un nuevo servicio siguiendo el patrón del proyecto Fresh.

## Proceso

### 1. Crear la Interfaz (Fresh.Core/Interfaces)

```csharp
// Fresh.Core/Interfaces/I{Entidad}Service.cs
using Fresh.Core.DTOs.{Entidad};

namespace Fresh.Core.Interfaces;

public interface I{Entidad}Service
{
    Task<IEnumerable<{Entidad}Response>> GetAllAsync();
    Task<{Entidad}Response?> GetByIdAsync(int id);
    Task<{Entidad}Response> CreateAsync({Entidad}Request request);
    Task<{Entidad}Response?> UpdateAsync(int id, {Entidad}Request request);
    Task<bool> DeleteAsync(int id);
}
```

### 2. Implementar el Servicio (Fresh.Infrastructure/Services)

```csharp
// Fresh.Infrastructure/Services/{Entidad}Service.cs
using Fresh.Core.DTOs.{Entidad};
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class {Entidad}Service : I{Entidad}Service
{
    private readonly FreshDbContext _context;

    public {Entidad}Service(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<{Entidad}Response>> GetAllAsync()
    {
        var items = await _context.{EntidadPlural}
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return items.Select(MapToResponse);
    }

    public async Task<{Entidad}Response?> GetByIdAsync(int id)
    {
        var item = await _context.{EntidadPlural}.FindAsync(id);
        return item is not null ? MapToResponse(item) : null;
    }

    public async Task<{Entidad}Response> CreateAsync({Entidad}Request request)
    {
        var entity = new {Entidad}
        {
            // Mapear desde request
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.{EntidadPlural}.Add(entity);
        await _context.SaveChangesAsync();

        return MapToResponse(entity);
    }

    public async Task<{Entidad}Response?> UpdateAsync(int id, {Entidad}Request request)
    {
        var entity = await _context.{EntidadPlural}.FindAsync(id);
        if (entity is null) return null;

        // Actualizar campos
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToResponse(entity);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _context.{EntidadPlural}.FindAsync(id);
        if (entity is null) return false;

        _context.{EntidadPlural}.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    private static {Entidad}Response MapToResponse({Entidad} entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
```

### 3. Registrar en Program.cs

```csharp
// Fresh.Api/Program.cs
builder.Services.AddScoped<I{Entidad}Service, {Entidad}Service>();
```

## Variaciones Comunes

### Con Relaciones (Include)

```csharp
public async Task<IEnumerable<OrderResponse>> GetAllAsync()
{
    var orders = await _context.Orders
        .Include(o => o.Customer)
        .Include(o => o.OrderItems)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
    return orders.Select(MapToResponse);
}
```

### Con Filtros

```csharp
public async Task<IEnumerable<ExpenseResponse>> GetByMonthAsync(int year, int month)
{
    var expenses = await _context.Expenses
        .Where(e => e.CreatedAt.Year == year && e.CreatedAt.Month == month)
        .OrderByDescending(e => e.CreatedAt)
        .ToListAsync();
    return expenses.Select(MapToResponse);
}
```

### Validación de Duplicados

```csharp
public async Task<{Entidad}Response> CreateAsync({Entidad}Request request)
{
    var exists = await _context.{EntidadPlural}
        .AnyAsync(x => x.Name.ToLower() == request.Name.ToLower());
    
    if (exists)
        throw new InvalidOperationException("Ya existe un registro con ese nombre");

    // ... resto del código
}
```

### Soft Delete

```csharp
public async Task<bool> DeleteAsync(int id)
{
    var entity = await _context.{EntidadPlural}.FindAsync(id);
    if (entity is null) return false;

    entity.IsActive = false;
    entity.UpdatedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return true;
}
```

## Lista de Verificación

- [ ] Interfaz creada en Fresh.Core/Interfaces
- [ ] Servicio implementado en Fresh.Infrastructure/Services
- [ ] Registrado en Program.cs como Scoped
- [ ] Métodos async con sufijo Async
- [ ] Mapeo manual Entity ↔ Response
- [ ] Inyección de dependencias vía constructor
