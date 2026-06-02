# Sistema de Logs - Fresh API

## Visión general

Cada petición HTTP que llega al API es registrada automáticamente en la base de datos PostgreSQL.
El sistema tiene dos vías de escritura: el middleware automático y el endpoint manual.

```
HTTP Request
    │
    ▼
ApiLoggingMiddleware        ← intercepta TODA petición
    │
    ├── Excluye: OPTIONS, /api/logs, /swagger, /hub, /presence, /negotiate
    │
    ▼
Ejecuta el pipeline normal  ← await _next(context)
    │
    ▼ (bloque finally)
Extrae: method, path, statusCode, userId del JWT
    │
    ▼ (Task.Run — no bloquea la respuesta)
SaveSimpleLogAsync → FreshDbContext → tabla `logs`
```

---

## Archivos involucrados

| Archivo | Rol |
|---|---|
| `Fresh.Api/Middleware/ApiLoggingMiddleware.cs` | Intercepta y registra cada request HTTP |
| `Fresh.Core/Entities/Log.cs` | Entidad de dominio |
| `Fresh.Core/Interfaces/ILogService.cs` | Contrato del servicio |
| `Fresh.Core/DTOs/Log/LogRequest.cs` | DTO para crear log vía API |
| `Fresh.Core/DTOs/Log/LogFilterRequest.cs` | Filtros + paginación para consultar |
| `Fresh.Core/DTOs/Log/LogResponse.cs` | DTO de respuesta |
| `Fresh.Core/DTOs/Log/PagedLogResponse.cs` | Respuesta paginada |
| `Fresh.Infrastructure/Services/LogService.cs` | Implementación CRUD |
| `Fresh.Api/Controllers/LogsController.cs` | Endpoints REST |
| `Fresh.Infrastructure/Data/FreshDbContext.cs` | Mapeo EF Core → tabla `logs` |

Registro de dependencia en `Program.cs`:
```csharp
builder.Services.AddScoped<ILogService, LogService>();   // línea ~23
app.UseMiddleware<ApiLoggingMiddleware>();                // línea ~250
```

---

## Middleware — ApiLoggingMiddleware

### Qué registra (modo rápido — SaveSimpleLogAsync)

| Campo log | Valor |
|---|---|
| `TransactionId` | `Guid.NewGuid()` por cada request |
| `LogDate` / `CreatedAt` | `DateTimeOffset.UtcNow` |
| `LogLevel` | `"ERROR"` si statusCode ≥ 400, `"INFO"` si no |
| `Operation` | `"GET /api/orders"` (method + path) |
| `TransactionStatus` | `"SUCCESS"` si 2xx, `"ERROR"` si no |
| `UserId` | Extraído del JWT claim `NameIdentifier` |
| `Message` | `"Respuesta rápida: 200"` |
| `DurationMs` | Siempre 0 en modo simple |

### Rutas excluidas (no se loguean)

```csharp
"/api/logs"     // evita loop infinito
"/swagger"
"/favicon.ico"
// también: paths con /hub, /presence, /negotiate (SignalR)
// también: método OPTIONS (preflight CORS)
```

### Por qué Task.Run

El log se guarda **después** de enviar la respuesta al cliente (`finally`), dentro de un `Task.Run`.
Esto evita que una escritura lenta en BD retrase la respuesta HTTP.
Se pasan solo variables primitivas al `Task.Run`; nunca el `HttpContext` (ya descartado al finalizar).

### Método SaveLogAsync (completo — no usado actualmente)

El middleware también tiene un método `SaveLogAsync` más detallado que incluye
`CorrelationId`, `EntityName`, `EntityId`, `Exception`, `TransactionData` y `DurationMs` real.
Está definido pero **no está siendo llamado**; `SaveSimpleLogAsync` es el que se usa.
Puede activarse para registrar cuerpos de request/response si se necesita auditoría completa.

---

## Entidad Log y tabla `logs`

```
Columna BD (snake_case)   →  Propiedad C# (PascalCase)
───────────────────────────────────────────────────────
id                        →  Id               (long, PK, autoincrement)
transaction_id            →  TransactionId    (string, required, max 100)
correlation_id            →  CorrelationId    (string?, max 100)
log_date                  →  LogDate          (DateTimeOffset, default NOW())
log_level                 →  LogLevel         (string, required, max 20)
operation                 →  Operation        (string?, max 100)  ← "METHOD /path"
entity_name               →  EntityName       (string?, max 100)  ← segmento [1] del path
entity_id                 →  EntityId         (string?, max 100)  ← segmento [2] del path
user_id                   →  UserId           (string?, max 100)
transaction_status        →  TransactionStatus(string?, max 30)   ← "SUCCESS" | "ERROR"
duration_ms               →  DurationMs       (int?)
logger                    →  Logger           (string?, max 255)
message                   →  Message          (string?)
exception                 →  Exception        (string?)
transaction_data          →  TransactionData  (string?)           ← JSON arbitrario
created_at                →  CreatedAt        (DateTimeOffset, default NOW())
```

**Importante:** Todos los campos tienen `HasColumnName` explícito en `FreshDbContext`.
Si se añade una propiedad nueva a `Log.cs`, debe registrarse también en `FreshDbContext` o EF Core
buscará la columna en PascalCase y fallará al guardar.

---

## LogService — métodos

### GetAllAsync(LogFilterRequest filter)

Consulta con filtros opcionales y paginación:

| Filtro | Campo en BD | Tipo de match |
|---|---|---|
| `LogLevel` | `log_level` | Exacto |
| `EntityName` | `entity_name` | Exacto |
| `UserId` | `user_id` | Exacto |
| `TransactionStatus` | `transaction_status` | Exacto |
| `TransactionId` | `transaction_id` | Exacto |
| `HttpMethod` | `operation` | StartsWith `"GET "` |
| `Operation` | `operation` | Contains |
| `From` / `To` | `log_date` | Rango DateTimeOffset |

Paginación: `Page` (default 1), `PageSize` (default 20).
Retorna `PagedLogResponse` con `TotalCount`, `TotalPages`, `Items`.

### GetByIdAsync(long id)

Busca por PK. Retorna `null` si no existe.

### CreateAsync(LogRequest request)

Permite que otros sistemas o el propio frontend registren logs manualmente.
Campos requeridos: `TransactionId`, `LogLevel`.

### DeleteAsync(long id)

Solo accesible por rol `admin` (restricción en el controlador).
Retorna `false` si el log no existe.

---

## LogsController — endpoints

Base: `GET|POST|DELETE /api/logs`  — requiere JWT (`[Authorize]`)

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/logs` | Lista paginada con filtros (query params) |
| GET | `/api/logs/{id}` | Log por ID |
| POST | `/api/logs` | Crear log manualmente |
| DELETE | `/api/logs/{id}` | Eliminar — solo rol `admin` |

---

## Flujo completo de una petición (ejemplo POST /api/orders)

```
1. Browser → POST /api/orders  (con JWT)
2. JwtMiddleware valida el token
3. ApiLoggingMiddleware.InvokeAsync:
     - EnableBuffering() en el request
     - await _next(context)  → ejecuta el controller
4. Controller crea la orden, retorna 201
5. finally block extrae: method="POST", path="/api/orders",
     statusCode=201, userId="42"
6. Task.Run (background, no bloquea la respuesta):
     - Crea Log { TransactionId=uuid, LogLevel="INFO",
         Operation="POST /api/orders", TransactionStatus="SUCCESS",
         UserId="42", Message="Respuesta rápida: 201" }
     - dbContext.Logs.Add(log) → INSERT INTO logs (...)
7. Cliente recibe 201 (Task.Run aún puede estar corriendo)
```

---

## Notas de diseño y limitaciones actuales

- **DurationMs siempre es 0** en el modo simple. Para medir tiempo real se necesita un `Stopwatch` antes del `await _next(context)`.
- **EntityName y EntityId no se llenan** en el modo simple. La lógica de extracción (`ExtractEntityName`, `ExtractEntityId`) está implementada pero solo se usa en `SaveLogAsync` (no activo).
- **Request body no se captura** en el modo simple. `ReadRequestBodyAsync` existe pero no se llama.
- **No hay retención automática**: los logs crecen indefinidamente. No hay job de limpieza ni TTL configurado.
- El endpoint `DELETE /api/logs/{id}` elimina de uno en uno; no hay borrado masivo por rango de fechas.
