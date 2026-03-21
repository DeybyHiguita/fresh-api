# Fresh API

Backend del proyecto Fresh, construido con **.NET 8**.

## Estructura

```
Fresh.Api/           → Proyecto principal (Controllers, Middleware)
Fresh.Core/          → DTOs, Entities, Interfaces
Fresh.Infrastructure/→ Data (DbContext), Services
Fresh.Database/      → Proyecto de base de datos
```

## Requisitos

- .NET 8 SDK
- Docker Desktop (opcional, para contenedores)

## Ejecutar en desarrollo

```bash
cd Fresh.Api
dotnet run
```

La API estará disponible en `http://localhost:5058`  
Swagger: `http://localhost:5058/swagger/index.html`

## Ejecutar con Docker

```bash
docker build -t fresh-api .
docker run -p 5058:8080 fresh-api
```

## Repositorios relacionados

- [fresh-app](https://github.com/DeybyHiguita/fresh-app) — Frontend Angular
- [fresh-db](https://github.com/DeybyHiguita/fresh-db) — Scripts SQL
