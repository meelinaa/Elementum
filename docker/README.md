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
| **REST API** | `api` | `8080` | `5000` | `Elementum.Api` service exposing REST endpoints and `/health` |
| **Worker** | `worker` | `8080` | `5094` | `Elementum.Worker` background daemon |

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
