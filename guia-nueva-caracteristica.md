# Guía: Crear una Nueva Característica en el API

## Paso 1: Planificación y Análisis

Antes de empezar a codificar, define:

1. **¿Qué va a hacer?** Descripción clara de la funcionalidad
2. **¿Qué datos necesita?** Entidades y relaciones con modelos existentes
3. **¿Qué operaciones?** CRUD, cálculos, validaciones especiales
4. **¿Quién puede acceder?** Validar roles y autenticación

**Ejemplo:** Agregar módulo de "Categorías" (ya existe, pero usamos como referencia)

---

## Paso 2: Crear/Actualizar la Entidad (Fresh.Core → Entities)

La entidad representa la estructura de datos en la base de datos.

**Archivo:** `Fresh.Core/Entities/Category.cs`

```csharp
namespace Fresh.Core.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Relaciones
    public ICollection<Recipe>? Recipes { get; set; }
}
```

**Consideraciones:**
- Usa propiedades auto-implementadas
- Incluye `Id` como clave primaria
- Agrega `CreatedAt` y `UpdatedAt` para auditoría
- Define relaciones con otras entidades

---

## Paso 3: Crear DTOs (Fresh.Core → DTOs)

Los DTOs (Data Transfer Objects) son lo que recibes/envías hacia el cliente. Separa la lógica de presentación.

**Carpeta:** `Fresh.Core/DTOs/Category/`

**Archivo:** `CategoryRequest.cs` (para POST/PUT)

```csharp
namespace Fresh.Core.DTOs.Category;

public class CategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

**Archivo:** `CategoryResponse.cs` (para GET)

```csharp
namespace Fresh.Core.DTOs.Category;

public class CategoryResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## Paso 4: Crear la Interfaz del Servicio (Fresh.Core → Interfaces)

Define que operaciones va a hacer tu servicio.

**Archivo:** `Fresh.Core/Interfaces/ICategoryService.cs`

```csharp
using Fresh.Core.DTOs.Category;

namespace Fresh.Core.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync();
    Task<CategoryResponse?> GetByIdAsync(int id);
    Task<CategoryResponse> CreateAsync(CategoryRequest request);
    Task<CategoryResponse?> UpdateAsync(int id, CategoryRequest request);
    Task<bool> DeleteAsync(int id);
}
```

---

## Paso 5: Implementar el Servicio (Fresh.Infrastructure → Services)

Aquí va la lógica de negocio. El servicio interactúa con la base de datos.

**Archivo:** `Fresh.Infrastructure/Services/CategoryService.cs`

```csharp
using Fresh.Core.DTOs.Category;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class CategoryService : ICategoryService
{
    private readonly FreshDbContext _context;

    public CategoryService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryResponse>> GetAllAsync()
    {
        var categories = await _context.Categories.ToListAsync();
        return categories.Select(MapToResponse);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        return category != null ? MapToResponse(category) : null;
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, CategoryRequest request)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return null;

        category.Name = request.Name;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;

        _context.Categories.Update(category);
        await _context.SaveChangesAsync();

        return MapToResponse(category);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return true;
    }

    private static CategoryResponse MapToResponse(Category category)
    {
        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
```

---

## Paso 6: Registrar el Servicio en Program.cs

Para que la inyección de dependencias funcione:

**Archivo:** [Fresh.Api/Program.cs](../Api/Fresh.Api/Program.cs)

Busca la sección de servicios y agrega:

```csharp
// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();  // ← Agrega esto
```

---

## Paso 7: Crear el Controlador (Fresh.Api → Controllers)

El controlador expone los endpoints HTTP.

**Archivo:** `Fresh.Api/Controllers/CategoriesController.cs`

```csharp
using Fresh.Core.DTOs.Category;
using Fresh.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fresh.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Obtiene todas las categorías
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Obtiene una categoría por ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category == null)
            return NotFound("Categoría no encontrada");

        return Ok(category);
    }

    /// <summary>
    /// Crea una nueva categoría
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create([FromBody] CategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = await _categoryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    /// <summary>
    /// Actualiza una categoría existente
    /// </summary>
    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryResponse>> Update(int id, [FromBody] CategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = await _categoryService.UpdateAsync(id, request);
        if (category == null)
            return NotFound("Categoría no encontrada");

        return Ok(category);
    }

    /// <summary>
    /// Elimina una categoría
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _categoryService.DeleteAsync(id);
        if (!result)
            return NotFound("Categoría no encontrada");

        return NoContent();
    }
}
```

---

## Paso 8: Agregar la Entidad al DbContext

El DbContext es donde registras todas tus entidades para EF Core.

**Archivo:** `Fresh.Infrastructure/Data/FreshDbContext.cs`

Busca y agrega tu DbSet:

```csharp
public DbSet<Category> Categories { get; set; }
public DbSet<Recipe> Recipes { get; set; }
public DbSet<User> Users { get; set; }
```

---

## Paso 9: Crear una Migración de Base de Datos

Las migraciones versionan los cambios en tu esquema de BD.

**En la terminal (desde carpeta Api):**

```bash
dotnet ef migrations add AddCategories --project Fresh.Infrastructure --startup-project Fresh.Api
```

Esto crea un archivo en `Fresh.Infrastructure/Data/Migrations/` con los cambios.

**Verificar la migración:**

Abre el archivo generado y valida que las columnas sean correctas. Luego ejecuta:

```bash
dotnet ef database update --project Fresh.Infrastructure --startup-project Fresh.Api
```

---

## Paso 10: Probar los Endpoints

**Opción 1: Swagger (UI interactiva)**

1. Ejecuta el proyecto: `dotnet run --project Fresh.Api`
2. Abre: `https://localhost:7123/swagger/index.html`
3. Prueba los endpoints directamente desde el navegador

**Opción 2: Fresh.Api.http (VS Code REST Client)**

Crea solicitudes en `Fresh.Api/Fresh.Api.http`:

```http
### Crear categoría
POST http://localhost:5000/api/categories
Content-Type: application/json

{
  "name": "Bebidas Frías",
  "description": "Jugos y sodas"
}

### Obtener todas
GET http://localhost:5000/api/categories

### Obtener por ID
GET http://localhost:5000/api/categories/1

### Actualizar
PUT http://localhost:5000/api/categories/1
Content-Type: application/json

{
  "name": "Bebidas Frías Actualizadas",
  "description": "Jugos, sodas y más"
}

### Eliminar
DELETE http://localhost:5000/api/categories/1
```

---

## Checklist: Crear una Nueva Característica

- [ ] Crear Entidad (Fresh.Core/Entities)
- [ ] Crear DTOs Request/Response (Fresh.Core/DTOs)
- [ ] Crear Interfaz del Servicio (Fresh.Core/Interfaces)
- [ ] Implementar el Servicio (Fresh.Infrastructure/Services)
- [ ] Registrar servicio en Program.cs
- [ ] Crear Controlador (Fresh.Api/Controllers)
- [ ] Agregar DbSet en FreshDbContext
- [ ] Crear migración: `dotnet ef migrations add NombreMigracion`
- [ ] Ejecutar migración: `dotnet ef database update`
- [ ] Probar endpoints en Swagger o REST Client
- [ ] Commit con mensaje claro: `feat: add categories module`

---

## Ejemplo de Mensaje de Commit

```
feat: add categories module

- Create Category entity with relationships
- Create CategoryRequest/Response DTOs
- Implement ICategoryService with CRUD operations
- Add CategoriesController with endpoints
- Create EF Core migration for categories table
- Add Swagger documentation for all endpoints
```

---

## Validaciones Comunes a Agregar

```csharp
// En el servicio o DTO:

// Validación de nulidad
if (string.IsNullOrWhiteSpace(request.Name))
    throw new ArgumentException("El nombre es requerido");

// Validación de duplicados
var exists = await _context.Categories
    .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower());
if (exists)
    throw new InvalidOperationException("Categoría ya existe");

// Validación de longitud
if (request.Name.Length > 100)
    throw new ArgumentException("El nombre no puede exceder 100 caracteres");
```

---

## Estructura Visual del Flujo

```
┌─────────────────────────────────────────────────────────────┐
│                    NUEVA CARACTERÍSTICA                      │
└─────────────────────────────────────────────────────────────┘

1. Entity (Fresh.Core/Entities)
   ↓
2. DTOs: Request + Response (Fresh.Core/DTOs)
   ↓
3. Interface Servicio (Fresh.Core/Interfaces)
   ↓
4. Implementar Servicio (Fresh.Infrastructure/Services)
   ↓
5. Registrar en Program.cs (inyección de dependencias)
   ↓
6. Controlador + Endpoints (Fresh.Api/Controllers)
   ↓
7. DbSet en FreshDbContext
   ↓
8. Migración EF Core (comando dotnet ef)
   ↓
9. Ejecutar migración (dotnet ef database update)
   ↓
10. Probar en Swagger/REST Client
```

---

## Consejos Prácticos

### Mantén la Coherencia
- Nombres en singular para entidades: `Category`, `Recipe`, `User`
- Nombres en plural para endpoints: `/api/categories`, `/api/recipes`, `/api/users`
- DTOs con sufijos: `CategoryRequest`, `CategoryResponse`

### Errores Comunes
- ❌ Olvidar registrar el servicio en `Program.cs` → Error de inyección de dependencias
- ❌ No crear la migración → Tabla no existe en BD
- ❌ Olvidar `[Authorize]` en operaciones sensibles → Seguridad débil
- ❌ Retornar la entidad directamente en lugar del DTO → Exposición innecesaria de datos

### Desde la Terminal
```bash
# Verificar si compilador está bien
cd Api
dotnet build

# Ver migraciones pendientes
dotnet ef migrations list

# Revertir última migración
dotnet ef migrations remove

# Ver estado de BD
dotnet ef dbcontext info
```

---

*Última actualización: 20 de marzo de 2026*
