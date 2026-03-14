# Elementum-Database & API (Docker)

A single Docker Compose setup for **MySQL** and **Elementum-ServiceApi**. Run from this folder.

## Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose

## Setup

1. **Start** (from the `Elementum-Database` folder):

   ```bash
   docker compose up -d
   ```

- **API:** http://localhost:5000 (e.g. `/api`, `/health`)  
  Port can be changed via `API_PORT` in `.env`.
- **MySQL:** `localhost:${MYSQL_PORT}` (default e.g. 3307); container port is always 3306 inside the network.

2. **Logs**  
   API: `docker compose logs -f api`  
   MySQL: `docker compose logs -f mysql`

3. **Stop**  
   `docker compose down`  
   Data is kept in the `mysql_data` volume.  
   To remove everything including data: `docker compose down -v`.

## After changing your project

- **Code changes (API, Worker, etc.):** Rebuild the image so the new code is used:
  ```bash
  docker compose up -d --build
  ```
  Or `docker compose down` then `docker compose up -d --build`.

- **Only config (`.env`, `docker-compose.yml`):** Restart is enough:
  ```bash
  docker compose down
  docker compose up -d
  ```

- **Init scripts (`init/*.sql`):** They run only on first start. To run them again you must remove the volume (this **deletes all DB data**): `docker compose down -v` then `docker compose up -d`.

## Database only (without API)

To start only MySQL:

```bash
docker compose up -d mysql
```

The API will not be built or started.

## Database init

SQL files in `init/` are run automatically on **first** start (when the volume is empty); see `init/README.md`.  
To re-run init from scratch: remove the volume and start again (`docker compose down -v` then `docker compose up -d`).

## API locally (without Docker)

Set the connection string in `Elementum-ServiceApi` (e.g. in `appsettings.json` or env var `ConnectionStrings__DefaultConnection` / `CONNECTION_STRING`). If MySQL runs via Docker: host `localhost`, port from `.env` (e.g. 3307).
