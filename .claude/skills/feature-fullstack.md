# Skill: Feature Completa (Full-Stack)

## Descripción

Guía para implementar una funcionalidad completa de principio a fin: Base de datos → Backend → Frontend.

## Orden de Implementación

```
1. Base de Datos (fresh-db)
   └── Crear tabla SQL

2. Backend (fresh-api)
   ├── Entidad (Core/Entities)
   ├── DTOs (Core/DTOs)
   ├── Interfaz (Core/Interfaces)
   ├── Servicio (Infrastructure/Services)
   ├── DbSet (Infrastructure/Data)
   ├── Registro DI (Program.cs)
   └── Controller (Api/Controllers)

3. Frontend (fresh-app)
   ├── Modelo (core/models)
   ├── Servicio (core/services)
   ├── Componente (features/)
   ├── Diálogo (features/)
   └── Ruta (app.routes.ts)
```

## Ejemplo: Crear módulo "Promotions"

### Paso 1: Base de Datos

```sql
-- fresh-db/tables/XX_promotions.sql
CREATE TABLE IF NOT EXISTS promotions (
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(100)    NOT NULL,
    description     TEXT,
    discount_type   VARCHAR(20)     NOT NULL DEFAULT 'percent',
    discount_value  NUMERIC(10,2)   NOT NULL,
    start_date      DATE            NOT NULL,
    end_date        DATE            NOT NULL,
    is_active       BOOLEAN         NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ     NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_promotions_dates ON promotions (start_date, end_date);
CREATE INDEX ix_promotions_active ON promotions (is_active) WHERE is_active = TRUE;
```

### Paso 2: Backend - Entidad

```csharp
// Fresh.Core/Entities/Promotion.cs
namespace Fresh.Core.Entities;

public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
```

### Paso 3: Backend - DTOs

```csharp
// Fresh.Core/DTOs/Promotion/PromotionRequest.cs
namespace Fresh.Core.DTOs.Promotion;

public class PromotionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = "percent";
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

// Fresh.Core/DTOs/Promotion/PromotionResponse.cs
public class PromotionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Paso 4: Backend - Interfaz + Servicio

```csharp
// Fresh.Core/Interfaces/IPromotionService.cs
public interface IPromotionService
{
    Task<IEnumerable<PromotionResponse>> GetAllAsync();
    Task<IEnumerable<PromotionResponse>> GetActiveAsync();
    Task<PromotionResponse?> GetByIdAsync(int id);
    Task<PromotionResponse> CreateAsync(PromotionRequest request);
    Task<PromotionResponse?> UpdateAsync(int id, PromotionRequest request);
    Task<bool> ToggleActiveAsync(int id);
    Task<bool> DeleteAsync(int id);
}
```

### Paso 5: Backend - DbContext

```csharp
// Fresh.Infrastructure/Data/FreshDbContext.cs
public DbSet<Promotion> Promotions { get; set; }
```

### Paso 6: Backend - Program.cs

```csharp
builder.Services.AddScoped<IPromotionService, PromotionService>();
```

### Paso 7: Frontend - Modelo

```typescript
// src/app/core/models/promotion.model.ts
export interface Promotion {
  id: number;
  name: string;
  description?: string;
  discountType: 'percent' | 'fixed';
  discountValue: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  createdAt: string;
}

export interface PromotionRequest {
  name: string;
  description?: string;
  discountType: 'percent' | 'fixed';
  discountValue: number;
  startDate: string;
  endDate: string;
}
```

### Paso 8: Frontend - Servicio

```typescript
// src/app/core/services/promotion.service.ts
@Injectable({ providedIn: 'root' })
export class PromotionService {
  private apiUrl = `${environment.apiUrl}/promotions`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Promotion[]> {
    return this.http.get<Promotion[]>(this.apiUrl);
  }

  getActive(): Observable<Promotion[]> {
    return this.http.get<Promotion[]>(`${this.apiUrl}/active`);
  }

  create(request: PromotionRequest): Observable<Promotion> {
    return this.http.post<Promotion>(this.apiUrl, request);
  }

  update(id: number, request: PromotionRequest): Observable<Promotion> {
    return this.http.put<Promotion>(`${this.apiUrl}/${id}`, request);
  }

  toggleActive(id: number): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/toggle`, {});
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
```

### Paso 9: Frontend - Ruta

```typescript
// src/app/app.routes.ts
{
  path: 'promotions',
  component: PromotionsComponent,
  canActivate: [AuthGuard],
},
```

## Correspondencia de Nombres

| PostgreSQL | C# Entity | C# DTO | TypeScript |
|------------|-----------|--------|------------|
| `promotions` | `Promotion` | `PromotionResponse` | `Promotion` |
| `discount_type` | `DiscountType` | `DiscountType` | `discountType` |
| `start_date` | `StartDate` | `StartDate` | `startDate` |
| `created_at` | `CreatedAt` | `CreatedAt` | `createdAt` |

## Checklist de Implementación

### Base de Datos
- [ ] Script SQL creado con tabla e índices
- [ ] Agregado a init.sql
- [ ] Ejecutado contra la BD

### Backend
- [ ] Entidad en Fresh.Core/Entities
- [ ] DTOs Request y Response
- [ ] Interfaz del servicio
- [ ] Implementación del servicio
- [ ] DbSet en FreshDbContext
- [ ] Registro en Program.cs
- [ ] Controller con CRUD completo
- [ ] Compilación sin errores

### Frontend
- [ ] Interfaces TypeScript
- [ ] Servicio HTTP
- [ ] Componente principal
- [ ] Diálogo de crear/editar
- [ ] Ruta registrada
- [ ] Prueba manual completa

## Comandos de Verificación

```bash
# Backend
cd fresh-api
dotnet build Fresh.sln
dotnet run --project Fresh.Api

# Frontend
cd fresh-app
ng serve

# Base de datos
cd fresh-db
./run.sh tables/XX_promotions.sql
```
