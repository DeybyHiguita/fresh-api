# ── Fresh API — Makefile ─────────────────────────────────
# Uso: make <comando>
# Prerequisito: Docker Desktop corriendo

.PHONY: up down restart build rebuild logs status clean

## Levantar el API
up:
	docker compose up -d

## Detener el API
down:
	docker compose down

## Reiniciar sin reconstruir
restart:
	docker compose restart

## Reconstruir y levantar (~11s)
build:
	docker compose build
	docker compose up -d

## Reconstruir desde cero (sin caché)
rebuild:
	docker compose build --no-cache
	docker compose up -d

## Ver logs en tiempo real
logs:
	docker compose logs -f

## Estado del contenedor
status:
	@docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" --filter "name=fresh-api"

## Eliminar contenedor + imagen
clean:
	docker compose down --rmi all --volumes
