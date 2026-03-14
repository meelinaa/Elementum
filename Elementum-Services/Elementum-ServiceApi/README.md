# Elementum-ServiceApi

ASP.NET Core API for Elementum (prices, health).

## Run locally

- Set the connection string in `appsettings.json` or via environment variable `CONNECTION_STRING` / `ConnectionStrings__DefaultConnection`.
- From the project folder: `dotnet run`.

## Docker / Docker Compose

A **single** setup for both database and API lives under **Elementum-Database**:

→ **[Elementum-Database/README.md](../../Elementum-Database/README.md)** — run `docker compose up -d` there.

The Dockerfile in this project is used by the Compose file in `Elementum-Database`.

## API endpoints

For a full description of all API routes, request/response shapes, and health checks, see **[API-Endpoints.md](API-Endpoints.md)**.
