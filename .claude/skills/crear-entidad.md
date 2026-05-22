# Skill: Crear Nueva Entidad

## Descripción

Este skill guía la creación de una nueva entidad en el sistema Fresh siguiendo el patrón Clean Architecture.

## Cuándo Usar

- Agregar un nuevo concepto de dominio al sistema
- Crear una nueva tabla que necesita representación en código

## Proceso Paso a Paso

### 1. Crear la Entidad (Fresh.Core/Entities)

```csharp
// Fresh.Core/Entities/{NombreEntidad}.cs
namespace Fresh.Core.Entities;

public class {NombreEntidad}
{
    public int Id { get; set; }
    
    // Propiedades específicas
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Foreign Keys
    public int UserId { get; set; }
    
    // Navegación (opcional)
    public User? User { get; set; }
    
    // Auditoría
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### 2. Crear los DTOs (Fresh.Core/DTOs/{Entidad}/)

**Request DTO** (para POST/PUT):

```csharp
// Fresh.Core/DTOs/{Entidad}/{Entidad}Request.cs
namespace Fresh.Core.DTOs.{Entidad};

public class {Entidad}Request
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int UserId { get; set; }
}
```

**Response DTO** (para GET):

```csharp
// Fresh.Core/DTOs/{Entidad}/{Entidad}Response.cs
namespace Fresh.Core.DTOs.{Entidad};

public class {Entidad}Response
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty; // Datos relacionados
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### 3. Agregar al DbContext

```csharp
// Fresh.Infrastructure/Data/FreshDbContext.cs

public DbSet<{NombreEntidad}> {NombreEntidadPlural} { get; set; }
```

### 4. Lista de Verificación

- [ ] Entidad creada con propiedades correctas
- [ ] DTOs Request y Response creados
- [ ] DbSet agregado al FreshDbContext
- [ ] Nombre en PascalCase singular para entidad
- [ ] Propiedades de auditoría incluidas (CreatedAt, UpdatedAt)
- [ ] Foreign Keys definidos si hay relaciones

## Tipos de Datos Comunes

| C# | PostgreSQL | Uso |
|----|------------|-----|
| `int` | `INTEGER` | IDs, cantidades |
| `decimal` | `NUMERIC(10,2)` | Dinero |
| `string` | `VARCHAR(n)` / `TEXT` | Texto |
| `bool` | `BOOLEAN` | Flags |
| `DateTime` | `TIMESTAMPTZ` | Fechas |
| `Guid` | `UUID` | Identificadores únicos |

## Ejemplo Completo

Para una entidad `Promotion`:

```csharp
// Entities/Promotion.cs
public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

// DTOs/Promotion/PromotionRequest.cs
public class PromotionRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

// DTOs/Promotion/PromotionResponse.cs
public class PromotionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```
