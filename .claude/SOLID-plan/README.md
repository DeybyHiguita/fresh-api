# Plan de Trabajo — Cumplimiento SOLID en Fresh API

## Contexto

Fresh API es una REST API en .NET 8 con Clean Architecture. La estructura de capas ya está bien definida, pero la implementación interna viola varios principios SOLID. Este plan cubre los cambios necesarios para llevarla a cumplimiento completo, ordenados por impacto vs esfuerzo.

## Estado actual por principio

| Principio | Estado | Problema principal |
|---|---|---|
| **S** — Single Responsibility | ❌ Violado | `OrderService` (432 líneas) mezcla validación, cálculos, transacciones y mapeo |
| **O** — Open/Closed | ⚠️ Parcial | Transiciones de estado con `if/switch` que requieren modificación para extender |
| **L** — Liskov Substitution | ✅ Cumple | Sin jerarquías de herencia problemáticas |
| **I** — Interface Segregation | ✅ Cumple | Interfaces por entidad, enfocadas |
| **D** — Dependency Inversion | ⚠️ Parcial | 36 servicios dependen directamente de `FreshDbContext` (clase concreta) |

## Fases del plan

Las fases están ordenadas por **ROI**: mayor impacto con menor riesgo primero.

| Fase | Principio | Archivo | Esfuerzo estimado |
|---|---|---|---|
| [Fase 1](./fase-1-exception-middleware.md) | SRP (Controllers) | Middleware de excepciones global | 2–4 horas |
| [Fase 2](./fase-2-single-responsibility.md) | SRP (Services) | Refactor de servicios grandes | 1–2 días |
| [Fase 3](./fase-3-repository-uow.md) | DIP | IRepository + IUnitOfWork | 2–3 días |
| [Fase 4](./fase-4-open-closed.md) | OCP | Transiciones de estado declarativas | 4–6 horas |

## Reglas durante el refactor

- Cada fase debe terminar con la API compilando y funcionando (no romper entre fases)
- No agregar nuevas funcionalidades durante el refactor
- Un PR por fase
- Los tests de integración existentes deben seguir pasando

## Qué NO cambiar

- La estructura de carpetas de Clean Architecture (ya es correcta)
- Los DTOs y entidades (ya siguen las convenciones)
- El mapeo manual Entity ↔ DTO (decisión de arquitectura explícita)
- Las interfaces existentes (ya cumplen ISP)
