# Fresh API

Backend del proyecto Fresh, construido con **.NET 8**.

## Estructura

```
Fresh.Api/            → Proyecto principal (Controllers, Middleware)
Fresh.Core/           → DTOs, Entities, Interfaces
Fresh.Infrastructure/ → Data (DbContext), Services
Fresh.Database/       → Proyecto de base de datos
Dockerfile            → Imagen Docker para producción
docker-compose.yml    → Orquestador del contenedor
Makefile              → Comandos rápidos
```

---

## Modo Desarrollo (sin Docker)

### Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Pasos

1. **Crear el archivo de configuración local** (solo la primera vez):

```bash
cp Fresh.Api/appsettings.json Fresh.Api/appsettings.Development.json
```

Edita `appsettings.Development.json` y agrega el connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-bitter-wildflower-amdy6uti-pooler.c-5.us-east-1.aws.neon.tech;Database=dbfresh;Username=neondb_owner;Password=TU_PASSWORD;SSL Mode=VerifyFull"
  }
}
```

2. **Ejecutar**:

```bash
cd Fresh.Api
dotnet run
```

3. **Verificar**:

| Recurso | URL |
|---------|-----|
| API | http://localhost:5058 |
| Swagger | http://localhost:5058/swagger/index.html |

> En modo desarrollo, Swagger se habilita automáticamente y los cambios en código se reflejan al reiniciar.

---

## Modo Docker (producción local)

### Requisitos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) abierto y corriendo

### Levantar

```bash
make up
```

### Reconstruir después de cambios en código

```bash
make build
```

### Reconstruir desde cero (sin caché)

```bash
make rebuild
```

### Ver logs en tiempo real

```bash
make logs
```

### Ver estado del contenedor

```bash
make status
```

### Detener

```bash
make down
```

### Verificar que funciona

```bash
curl -s http://localhost:5058/swagger/v1/swagger.json | python3 -c "
import sys, json
d = json.load(sys.stdin)
paths = sorted(d.get('paths', {}).keys())
print(f'{len(paths)} endpoints disponibles')
for p in paths[:5]:
    print(f'  {p}')
if len(paths) > 5:
    print(f'  ... y {len(paths)-5} más')
"
```

---

## Comandos rápidos (Makefile)

| Comando | Qué hace |
|---------|----------|
| `make up` | Levantar el contenedor |
| `make down` | Detener el contenedor |
| `make build` | Reconstruir imagen y levantar |
| `make rebuild` | Reconstruir sin caché y levantar |
| `make logs` | Ver logs en tiempo real |
| `make status` | Ver estado del contenedor |
| `make clean` | Eliminar contenedor + imagen |

---

## Repositorios relacionados

- [fresh-app](https://github.com/DeybyHiguita/fresh-app) — Frontend Angular
- [fresh-db](https://github.com/DeybyHiguita/fresh-db) — Scripts SQL
