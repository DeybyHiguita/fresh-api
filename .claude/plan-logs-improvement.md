# Plan: Mejora del Sistema de Logs

## Problema actual

El middleware `ApiLoggingMiddleware` solo guarda logs simples ("Respuesta rápida: 500").
Cuando ocurre un error real — por ejemplo Gemini retorna token inválido o Google Drive falla —
el log solo muestra status 500 y mensaje genérico. Los campos `exception`, `entityName`,
`entityId` y `durationMs` **nunca se llenan**.

Raíz del problema: `SaveSimpleLogAsync` se creó para evitar bloqueos, pero sacrificó
toda la información útil. `SaveLogAsync` (el método completo) existe pero jamás es llamado.

---

## Cambios en la API — `Fresh.Api` + `Fresh.Infrastructure`

### 1. `ApiLoggingMiddleware` — activar captura completa

**Archivo:** `Fresh.Api/Middleware/ApiLoggingMiddleware.cs`

Cambios:
- Agregar `Stopwatch` antes de `await _next(context)` para medir duración real.
- Envolver el pipeline en `try/catch` para capturar excepciones no controladas.
- Llamar a `ExtractEntityName` y `ExtractEntityId` (ya están codificados, nunca se usan).
- Para errores (status ≥ 400), capturar el body de la respuesta para guardarlo en `message`.
- Reemplazar llamada a `SaveSimpleLogAsync` por `SaveLogAsync` con todos los campos.
- En el bloque `catch`, guardar el `ex.ToString()` en el campo `exception`.

```
Antes (siempre):
  SaveSimpleLogAsync(method, path, statusCode, userId)
    → DurationMs = 0, EntityName = null, Exception = null

Después:
  SaveLogAsync(method, path, statusCode, userId,
               entityName, entityId, durationMs, exceptionText)
    → DurationMs = tiempo real, EntityName = "gemini",
      Exception = stack trace completo cuando aplique
```

### 2. Middleware global de excepciones — captura lo que el middleware no alcanza

**Archivo nuevo:** `Fresh.Api/Middleware/GlobalExceptionMiddleware.cs`

Algunos controllers lanzan excepciones que ASP.NET convierte en 500 ANTES de que el
`ApiLoggingMiddleware` pueda leerlas (por el orden del pipeline). Este middleware
se coloca al principio, hace `try/catch` de todo el pipeline, y guarda el log
con `LogLevel = "Fatal"` y el stack trace completo antes de relanzar.

Registro en `Program.cs`:
```csharp
app.UseMiddleware<GlobalExceptionMiddleware>();   // ← primero
app.UseMiddleware<ApiLoggingMiddleware>();
```

### 3. `GeminiController` — loguear errores de Gemini a la BD

**Archivo:** `Fresh.Api/Controllers/GeminiController.cs`

Cuando la llamada a Gemini no es exitosa (`!response.IsSuccessStatusCode`),
actualmente el error se devuelve al cliente pero **no se guarda en logs**.
El `_logger.LogWarning/LogError` escribe en consola, no en PostgreSQL.

Cambio: inyectar `ILogService` en el controlador y llamar `CreateAsync` cuando:
- Gemini retorna error (guardar `responseBody` en `transactionData`, `LogLevel = "Error"`).
- Google Drive upload falla (guardar `ex.ToString()` en `exception`).

```csharp
// En el catch de Drive o cuando !response.IsSuccessStatusCode:
await _logService.CreateAsync(new LogRequest {
    TransactionId  = Guid.NewGuid().ToString(),
    LogLevel       = "Error",
    Operation      = "POST /api/gemini/analyze-invoice",
    EntityName     = "gemini",
    TransactionStatus = "ERROR",
    Message        = "Gemini API error",
    Exception      = responseBody,          // el error real de la API de Google
    Logger         = nameof(GeminiController),
});
```

### 4. Interceptar body de respuesta para errores 4xx/5xx

**Archivo:** `Fresh.Api/Middleware/ApiLoggingMiddleware.cs`

Para poder leer el body de la respuesta (que tiene el mensaje de error real),
hay que intercambiar `context.Response.Body` por un `MemoryStream` antes de
`await _next(context)`, leer el contenido, y restaurarlo. Solo aplica cuando
status ≥ 400 (no penalizar el caso feliz).

Esto permite guardar en `message` el JSON que el controller devuelve, por ejemplo:
`{"message":"API key expired","detail":"..."}`

---

## Cambios en la App — `fresh-app`

### 5. Lista de logs — indicador visual de excepción

**Archivo:** `src/app/features/logs/logs.component.html`

Agregar en cada fila de la tabla un ícono `bug_report` (rojo) cuando `log.exception`
tiene contenido. Actualmente no hay ninguna diferencia visual entre un error
con excepción y uno sin ella.

### 6. Lista de logs — filtro "tiene excepción"

**Archivo:** `src/app/features/logs/logs.component.html` + `log.model.ts` + `log.service.ts`

Agregar checkbox/toggle "Solo con excepción" en la barra de filtros.
Requiere un parámetro nuevo `hasException: boolean` en `LogFilterRequest`.

**API — `LogFilterRequest.cs`:**
```csharp
public bool? HasException { get; set; }
```

**API — `LogService.GetAllAsync`:**
```csharp
if (filter.HasException == true)
    query = query.Where(l => l.Exception != null && l.Exception != "");
```

### 7. Detalle del log — copiar excepción y mejorar stack trace

**Archivo:** `src/app/features/logs/log-detail-dialog.component.ts`

El diálogo ya tiene sección de excepción (`pre-block--error`), pero:
- No hay botón de copiar al portapapeles.
- El stack trace de .NET es difícil de leer en bloque plano.

Agregar:
- Botón "Copiar" en la sección de excepción.
- Highlight de la primera línea (tipo de excepción) en negrita/rojo más intenso.

### 8. `log.model.ts` — alinear con backend

**Archivo:** `src/app/core/models/log.model.ts`

Verificar que `LogFilter` tenga el campo `hasException?: boolean` para
enviarlo como query param al API.

---

## Orden de implementación

```
Paso 1 — API: GlobalExceptionMiddleware (nuevo archivo)
Paso 2 — API: ApiLoggingMiddleware (activar SaveLogAsync con Stopwatch + entidades)
Paso 3 — API: LogFilterRequest + LogService (filtro hasException)
Paso 4 — API: GeminiController (inyectar ILogService, loguear errores reales)
Paso 5 — App: log.model.ts + log.service.ts (hasException en filtro)
Paso 6 — App: logs.component.html (ícono excepción + toggle filtro)
Paso 7 — App: log-detail-dialog (botón copiar + highlight)
```

---

## Resultado esperado

Cuando Gemini retorna "API key expired" o un error 403, la tabla de logs mostrará:

| Campo | Antes | Después |
|---|---|---|
| `logLevel` | INFO | ERROR |
| `message` | "Respuesta rápida: 500" | "Gemini API error" |
| `exception` | null | `{"error":{"code":403,"message":"API key expired",...}}` |
| `entityName` | null | "gemini" |
| `durationMs` | 0 | 2340 |
| `transactionData` | null | body completo del request |

Y en la pantalla de logs aparecerá el ícono `bug_report` rojo en esa fila,
al abrir el detalle verás el error exacto de Google en el bloque de excepción.
