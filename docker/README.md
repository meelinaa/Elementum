# Elementum — Docker Compose Deployment

Docker Compose stack for **MySQL 8**, **Elementum.Api**, and **Elementum.Worker** (background ingestion daemon).

---

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) & Docker Compose

---

## Quick Start

1. Navigate to this directory:
   ```bash
   cd docker
   cp .env.example .env
   ```
   Set `MYSQL_ROOT_PASSWORD`, `MYSQL_USER`, and `MYSQL_PASSWORD` in `.env` before starting.
2. Start the full stack in detached mode:
   ```bash
   docker compose up -d
   ```

---

## Services & Ports

| Service | Compose Name | Internal Port | Host Port (Default) | Description |
|---------|--------------|---------------|---------------------|-------------|
| **MySQL** | `mysql` | `3306` | `3307` | Database with automated table initialization from `init/` |
| **REST API** | `api` | `8080` | `5000` | `Elementum.Api` (HTTP on the Docker network; TLS via `proxy`) |
| **Worker** | `worker` | `5094` | `5094` | `Elementum.Worker` background daemon |
| **Reverse proxy** | `proxy` | `80` / `443` | `80` / `443` | Caddy — profile `edge` only; HTTPS termination |

---

## Production CORS & HTTPS

Compose runs the API with `ASPNETCORE_ENVIRONMENT=Production`. Browser origins must be listed explicitly; Development-style `AllowAnyOrigin` is not used.

1. Set `CORS_ALLOWED_ORIGIN` in `.env` to the frontend origin (e.g. `https://app.example.com`). Compose maps it to `Cors__AllowedOrigins__0`.
2. TLS terminates at the reverse proxy, not in Kestrel:
   ```bash
   docker compose --profile edge up -d
   ```
   Caddy (`docker/Caddyfile`) listens on 80/443 and proxies to `api:8080`. Set `SITE_ADDRESS=https://api.example.com` for public Let's Encrypt; `https://localhost` uses Caddy's local TLS.
3. The API sets `ReverseProxy__TerminateHttps=true` so it does **not** HTTP→HTTPS-redirect on the internal port. `X-Forwarded-For` / `X-Forwarded-Proto` are honored.

Do not expose `API_PORT` on a public interface when `proxy` is the edge; keep 8080 on the Docker network.

---

## Health checks

| Service | Probe | Path |
|---------|--------|------|
| **API** | Compose `healthcheck` | `GET /health/live` (process-up; `/health/ready` still checks MySQL) |
| **Worker** | Compose `healthcheck` | `GET /health/live` (process-up; `GET /health` still runs DB + upstream checks) |

Images use floating tags (`mysql:8`, `redis:7-alpine`, `caddy:2-alpine`, `dotnet/sdk:10.0`). Pin by digest when a release must be bit-reproducible.

---

## Common Commands

### View Logs
```bash
# Follow API logs
docker compose logs -f api

# Follow Worker logs
docker compose logs -f worker

# Follow MySQL logs
docker compose logs -f mysql

# Combined log stream
docker compose logs -f
```

### Stop Services
```bash
# Stop containers (preserves database data)
docker compose down

# Stop and wipe database volume
docker compose down -v
```

### Rebuild After Code Changes
```bash
docker compose up -d --build
```

---

## Related Documentation

- [Root README](../README.md) — Overall project architecture and getting started guide
- [Elementum.Api Endpoints](../src/Elementum.Api/API-Endpoints.md) — API route reference
