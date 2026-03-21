# Guía: Crear una Nueva Característica en la API — Fresh

## Arquitectura del Proyecto

```
Fresh.Core/            → Contratos (Entidades, DTOs, Interfaces)
Fresh.Infrastructure/  → Implementación (Servicios, DbContext)
Fresh.Api/             → Presentación (Controladores, Middleware)
```

---

## Paso 1: Crear la Entidad (`Fresh.Core → Entities`)

La entidad representa la tabla en la base de datos.

**Convenciones:**
- Nombre en **singular**: `Product`, `WorkShift`, `MenuItem`
- Incluye siempre `Id`, `CreatedAt`, `UpdatedAt`
- Usa `DateOnly` para fechas sin hora, `DateTimeOffset` para timestamps con zona horaria (PostgreSQL `TIMESTAMPTZ`)
- Define las relaciones de navegación

**Ejemplo real — `Fresh.Core/Entities/Product.cs`:**
```csharp
namespace Fresh.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UnitMeasure { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = [];
}
```

> Si la entidad se basa en una **vista SQL** (solo lectura), usa `HasNoKey()` en el DbContext. Ver ejemplo en `DailyWorkedHours`.

---

## Paso 2: Crear los DTOs (`Fresh.Core → DTOs/{Modulo}/`)

Crea una carpeta por módulo. Siempre dos archivos mínimo:

| Archivo | Uso | Verbo HTTP |
|---|---|---|
| `{Entidad}Request.cs` | Lo que recibe la API | `POST`, `PUT` |
| `{Entidad}Response.cs` | Lo que devuelve la API | `GET` |

Agrega `[Required]`, `[MaxLength]` y `[Range]` en el Request para validación automática.

**Ejemplo real — `Fresh.Core/DTOs/Product/ProductRequest.cs`:**
```csharp
using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Product;

public class ProductRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string UnitMeasure { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal CurrentStock { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
```

---

## Paso 3: Crear la Interfaz (`Fresh.Core → Interfaces/`)

Define el contrato del servicio. **Solo firmas, sin lógica.**

**Ejemplo real — `Fresh.Core/Interfaces/IProductService.cs`:**
```csharp
using Fresh.Core.DTOs.Product;

namespace Fresh.Core.Interfaces;

public interface IProductService
{
    Task<IEnumerable<ProductResponse>> GetAllAsync(bool onlyActive = true);
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<ProductResponse> CreateAsync(ProductRequest request);
    Task<ProductResponse?> UpdateAsync(int id, ProductRequest request);
    Task<bool> DeleteAsync(int id);
}
```

---

## Paso 4: Implementar el Servicio (`Fresh.Infrastructure → Services/`)

Aquí va toda la lógica de negocio. El servicio accede a la BD a través del `FreshDbContext`.

**Convenciones:**
- Lanza `InvalidOperationException` para reglas de negocio rotas
- Lanza `KeyNotFoundException` cuando una entidad relacionada no existe
- Usa **soft delete** (`IsActive = false`) cuando los datos no deben eliminarse físicamente
- El `MapToResponse` siempre es `private static`

**Ejemplo real — `Fresh.Infrastructure/Services/ProductService.cs`:**
```csharp
using Fresh.Core.DTOs.Product;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly FreshDbContext _context;

    public ProductService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductResponse>> GetAllAsync(bool onlyActive = true)
    {
        var query = _context.Products.AsQueryable();
        if (onlyActive) query = query.Where(p => p.IsActive);
        var products = await query.OrderBy(p => p.Name).ToListAsync();
        return products.Select(MapToResponse);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? null : MapToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(ProductRequest request)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Name.ToLower() == request.Name.ToLower());
        if (exists)
            throw new InvalidOperationException($"Ya existe un producto con el nombre '{request.Name}'");

        var product = new Product
        {
            Name = request.Name,
            UnitMeasure = request.UnitMeasure,
            CurrentStock = request.CurrentStock,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return MapToResponse(product);
    }

    public async Task<ProductResponse?> UpdateAsync(int id, ProductRequest request)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        product.Name = request.Name;
        product.UnitMeasure = request.UnitMeasure;
        product.CurrentStock = request.CurrentStock;
        product.IsActive = request.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        // Soft delete
        product.IsActive = false;
        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return true;
    }

    private static ProductResponse MapToResponse(Product p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        UnitMeasure = p.UnitMeasure,
        CurrentStock = p.CurrentStock,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
```

---

## Paso 5: Registrar en `Program.cs` (`Fresh.Api`)

Agrega una línea por cada par interfaz → implementación.

**Estado actual del proyecto:**
```csharp
// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IPurchaseBatchService, PurchaseBatchService>();
builder.Services.AddScoped<IWorkShiftService, WorkShiftService>();
builder.Services.AddScoped<IMenuItemService, MenuItemService>();
// ← Agrega aquí el nuevo servicio
```

> ❌ **Error más común:** olvidar este paso genera `InvalidOperationException: Unable to resolve service` al primer request.

---

## Paso 6: Agregar el `DbSet` y configuración en `FreshDbContext`

**Archivo:** `Fresh.Infrastructure/Data/FreshDbContext.cs`

Dos cosas obligatorias:

**1. Declarar el DbSet:**
```csharp
public DbSet<Product> Products => Set<Product>();
```

**2. Configurar el mapeo en `OnModelCreating`** (tabla, columnas, índices, relaciones):
```csharp
modelBuilder.Entity<Product>(entity =>
{
    entity.ToTable("products");
    entity.HasKey(e => e.Id);
    entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
    entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
    entity.Property(e => e.UnitMeasure).HasColumnName("unit_measure").HasMaxLength(50).IsRequired();
    entity.Property(e => e.CurrentStock).HasColumnName("current_stock").HasPrecision(10, 2).HasDefaultValue(0m);
    entity.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true);
    entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
    entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
    entity.HasIndex(e => e.Name).HasDatabaseName("ix_products_name");
});
```

**Para vistas SQL (solo lectura):**
```csharp
modelBuilder.Entity<DailyWorkedHours>(entity =>
{
    entity.ToView("vw_daily_worked_hours");
    entity.HasNoKey();
    entity.Property(e => e.ShiftId).HasColumnName("shift_id");
    // ...
});
```

---

## Paso 7: Crear el Controlador (`Fresh.Api → Controllers/`)

**Convenciones:**
- Nombre en **plural**: `ProductsController`, `WorkShiftsController`
- Ruta: `api/[controller]` → `/api/products`, `/api/workshifts`
- Usa `[Authorize]` en toda operación que modifique datos
- Captura excepciones del servicio y devuelve el código HTTP correcto:
  - `InvalidOperationException` → `409 Conflict`
  - `KeyNotFoundException` → `404 Not Found`
  - `ArgumentException` → `400 Bad Request`

**Ejemplo real — patrón de un endpoint con validación:**
```csharp
[Authorize]
[HttpPost]
public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    try
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
}
```

**Para endpoints de cierre de estado** (sin body), usa `[HttpPatch]`:
```csharp
// Ejemplo real: finalizar jornada
[HttpPatch("{id}/end")]
public async Task<ActionResult<WorkShiftResponse>> EndShift(int id) { ... }
```

**Para sub-recursos anidados** (relaciones padre→hijo):
```csharp
// Ejemplo real: agregar detalle a un lote de compra
[HttpPost("{id}/details")]
public async Task<ActionResult<PurchaseDetailResponse>> AddDetail(int id, [FromBody] PurchaseDetailRequest request) { ... }

// Ejemplo real: iniciar descanso dentro de una jornada
[HttpPost("{id}/breaks/start")]
public async Task<ActionResult<BreakTimeResponse>> StartBreak(int id, [FromBody] BreakTimeRequest request) { ... }
```

---

## Módulos actuales del proyecto

| Módulo | Tabla(s) BD | Controlador | Características especiales |
|---|---|---|---|
| **Auth** | `users` | `AuthController` | Login/Register con JWT |
| **Recipes** | `recipes`, `recipe_ingredients` | `RecipesController` | Ingredientes anidados |
| **Logs** | `logs` | `LogsController` | Auto-guardado vía `ApiLoggingMiddleware` |
| **Products** | `products` | `ProductsController` | Soft delete (`is_active`) |
| **PurchaseBatches** | `purchase_batches`, `purchase_details` | `PurchaseBatchesController` | Detalles anidados, actualiza stock |
| **WorkShifts** | `work_shifts`, `break_times` | `WorkShiftsController` | Descansos anidados, vista SQL |
| **MenuItems** | `menu_items` | `MenuItemsController` | CRUD simple |

---

## Middleware de Logging (`ApiLoggingMiddleware`)

Todos los endpoints se loguean automáticamente en la tabla `logs`. **No requiere ninguna acción del desarrollador.**

Rutas excluidas del log (para evitar recursión):
- `/api/logs`
- `/swagger`
- `/favicon.ico`

Datos que captura automáticamente por cada request:

| Campo | Valor |
|---|---|
| `transaction_id` | GUID único por request |
| `operation` | `"GET /api/products/5"` |
| `entity_name` | Extraído de la ruta (`products`) |
| `entity_id` | ID de la ruta o email del body (para auth) |
| `user_id` | Claim del JWT |
| `log_level` | `INFO` / `WARN` / `ERROR` según status HTTP |
| `transaction_status` | `SUCCESS` / `CLIENT_ERROR` / `SERVER_ERROR` |
| `duration_ms` | Tiempo real de respuesta |
| `message` | Mensaje de error del response para 4xx/5xx |

---

## Convenciones de nombres

| Elemento | Convención | Ejemplo |
|---|---|---|
| Entidad | Singular, PascalCase | `WorkShift` |
| Tabla BD | Plural, snake_case | `work_shifts` |
| Endpoint (ruta) | Plural, kebab-case | `/api/work-shifts` |
| DTO Request | `{Entidad}Request` | `WorkShiftRequest` |
| DTO Response | `{Entidad}Response` | `WorkShiftResponse` |
| Interfaz | `I{Entidad}Service` | `IWorkShiftService` |
| Servicio | `{Entidad}Service` | `WorkShiftService` |
| Controlador | `{Entidades}Controller` | `WorkShiftsController` |

---

## Checklist: Nueva Característica

- [ ] Entidad en `Fresh.Core/Entities/`
- [ ] DTOs `Request` y `Response` en `Fresh.Core/DTOs/{Modulo}/`
- [ ] Interfaz en `Fresh.Core/Interfaces/I{Entidad}Service.cs`
- [ ] Servicio en `Fresh.Infrastructure/Services/{Entidad}Service.cs`
- [ ] `AddScoped<I{Entidad}Service, {Entidad}Service>()` en `Program.cs`
- [ ] `DbSet` + configuración en `FreshDbContext.cs`
- [ ] Controlador en `Fresh.Api/Controllers/{Entidades}Controller.cs`
- [ ] Verificar que compila: `dotnet build`
- [ ] Probar en Swagger: `dotnet run --project Fresh.Api`

---

## Referencia rápida de tipos C# ↔ PostgreSQL

| PostgreSQL | C# |
|---|---|
| `SERIAL` / `BIGSERIAL` | `int` / `long` |
| `VARCHAR(n)` | `string` + `HasMaxLength(n)` |
| `TEXT` | `string` |
| `NUMERIC(p,s)` | `decimal` + `HasPrecision(p,s)` |
| `BOOLEAN` | `bool` |
| `DATE` | `DateOnly` |
| `TIMESTAMPTZ` | `DateTimeOffset` |
| `TIMESTAMP` | `DateTime` |
| `INTEGER` | `int` |

---

## Comandos útiles

```bash
# Compilar y verificar errores
dotnet build

# Ejecutar la API (Swagger en http://localhost:5058/swagger)
dotnet run --project Fresh.Api

# Crear migración (solo si se agregan tablas nuevas — las tablas actuales ya existen)
dotnet ef migrations add NombreMigracion --project Fresh.Infrastructure --startup-project Fresh.Api

# Aplicar migración
dotnet ef database update --project Fresh.Infrastructure --startup-project Fresh.Api
```

---

*Última actualización: junio 2025*

