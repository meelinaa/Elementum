# Elementum-Database, API & Worker (Docker)

A Docker Compose stack for **MySQL**, **Elementum-ServiceApi**, and **Elementum-WorkerService** (background ingestion). Run all commands from this folder (where `docker-compose.yml` lives).

Copy `.env.example` to `.env` and adjust values. The worker also reads `../Elementum-Services/Elementum-WorkerService/.env` (API keys, etc.) as configured in Compose.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

## Setup

1. **Start everything**

   ```bash
   docker compose up -d
   ```

2. **Services & endpoints**

   | Service | Compose name | Purpose |
   |--------|----------------|--------|
   | MySQL | `mysql` | Database; init scripts from `init/` on first run |
   | REST API | `api` | `Elementum-ServiceApi` |
   | Worker | `worker` | `Elementum-WorkerService` (scheduled jobs, writes to MySQL) |

   - **API:** `http://localhost:${API_PORT:-8080}` — e.g. `/api`, `/health`. Set **`API_PORT`** in `.env` (`.env.example` uses `5000`; if unset, Compose defaults to **8080**).
   - **Worker (HTTP):** `http://localhost:${WORKER_PORT:-5094}` — Kestrel URL from the worker image (health/metrics if exposed). Set **`WORKER_PORT`** in `.env` to change the host port.
   - **MySQL:** `localhost:${MYSQL_PORT}` (e.g. **3307**); inside Docker the port is always **3306**.

3. **Stop**

   ```bash
   docker compose down
   ```

   Data stays in the `mysql_data` volume. To remove containers **and** volumes (including DB data):

   ```bash
   docker compose down -v
   ```

## Logs (console)

Run these next to `docker-compose.yml` (or use `docker compose -f <path> ...`).

| Goal | Command |
|------|---------|
| **Follow** API logs | `docker compose logs -f api` |
| **Follow** MySQL logs | `docker compose logs -f mysql` |
| **Follow** Worker logs | `docker compose logs -f worker` |
| **Last N lines** (no follow) | `docker compose logs --tail=100 worker` |
| **Since** a time window | `docker compose logs -f --since 30m api` |
| **All services** (combined stream) | `docker compose logs -f` |

List service names:

```bash
docker compose config --services
```

## After changing the project

- **Code changes (API, worker, Dockerfiles):** rebuild images:

  ```bash
  docker compose up -d --build
  ```

  Or `docker compose down` then `docker compose up -d --build`.

- **Only config (`.env`, `docker-compose.yml`):** restart:

  ```bash
  docker compose down
  docker compose up -d
  ```

- **Init scripts (`init/*.sql`):** run only on **first** start with an empty DB volume. To run them again, remove the volume (**deletes all DB data**): `docker compose down -v` then `docker compose up -d`.

## Selective startup

- **Database only (no API, no worker):**

  ```bash
  docker compose up -d mysql
  ```

- **API + MySQL, without worker:**

  ```bash
  docker compose up -d mysql api
  ```

## Database init

SQL files in `init/` run automatically on **first** start (empty volume); see `init/README.md`.  
To re-run from scratch: `docker compose down -v` then `docker compose up -d`.

## API or worker locally (without Docker)

Point `ConnectionStrings__DefaultConnection` at your MySQL host (e.g. `localhost` and `MYSQL_PORT` from `.env`). Worker: same connection string plus its own `Elementum-WorkerService` configuration.

---

See [../docs/screenshots/README.md](../docs/screenshots/README.md) for a short checklist.

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](../Elementum-Services/Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- **This README** — Docker stack (API + DB + worker)
