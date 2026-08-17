<img width="1920" height="553" alt="Elementum" src="https://github.com/user-attachments/assets/ea4edcb2-94d2-41b8-8d60-de1f83949946" />


# Elementum

A full-stack .NET application for **precious metal price tracking** (gold, silver, platinum, palladium). It demonstrates a clean separation between data persistence, REST API, background ingestion, and a console client — suitable for portfolio or technical assessment contexts.

---

## Table of contents

- [Project overview](#project-overview)
- [Tech stack](#tech-stack)
- [Features](#features)
- [Architecture](#architecture)
- [Repository structure](#repository-structure)
- [Prerequisites](#prerequisites)
- [Getting started](#getting-started)
- [Running the components](#running-the-components)
- [Screenshots](#screenshots)
- [Related documentation](#related-documentation)

---

## Project overview

**Elementum** consists of four main parts:

| Component | Description |
|-----------|-------------|
| **docker** | MySQL schema, init scripts, and a single Docker Compose setup for the database, API, and worker. |
| **Elementum.ServiceApi** | ASP.NET Core REST API: metals catalog, price history (latest, by symbol, date range, aggregated), trading and karat-oriented endpoints. |
| **Elementum.WorkerService** | Background worker that fetches daily metal prices from an external API (e.g. GoldAPI) and persists them to the database. Can be run as a Windows Service. |
| **Elementum.Cli** | Console application that consumes the API and provides a menu-driven UI: dashboard, metal list, trading view, karat calculator, and history. |

The CLI communicates with the API; the API and the Worker share the same MySQL database. The system can be run entirely via Docker (database + API) or with the API and Worker running locally against your own MySQL instance.

---

## Tech stack

- **Runtime & framework:** .NET 10 (C#)
- **API:** ASP.NET Core (REST), health checks (liveness/readiness)
- **Data:** MySQL 8, Entity Framework Core, structured init scripts
- **Infrastructure:** Docker, Docker Compose
- **Client:** Console application (CLI) with configurable API base URL
- **Testing:** Dedicated test projects for API, Worker, and CLI (xUnit, Moq)

---

## Features

- **REST API** with versioned base path (`/api/v1`), validation, and clear DTOs for metals and price history
- **Flexible queries:** latest per metal, by symbol, by date range, and aggregated (daily/weekly/monthly/yearly)
- **Specialized views:** trading (bid/ask, OHLC) and karat (price per gram by purity) endpoints for the CLI
- **Docker-based deployment:** one Compose file for MySQL and API; database init on first run
- **Background ingestion:** worker with configurable schedule; installable as Windows Service
- **Console CLI:** keyboard-driven navigation, dashboard, metal selection, and multiple detail views

---

## Architecture

Data flows from an external price API into the Worker, which writes to MySQL. The ServiceApi reads from the same database and exposes REST endpoints. The CLI calls the API and renders the data in the console. The database schema (metals, price history with OHLC and karat-related fields) is created and seeded via SQL scripts in `docker/init/`, which run automatically on first container start.

---

## Repository structure

```
Elementum/
├── docker/                             # Docker Compose (MySQL + API + Worker), init scripts
│   ├── docker-compose.yml
│   ├── .env.example
│   ├── README.md
│   └── init/
│       ├── README.md
│       └── 01-schema.sql
├── src/                                # .NET source projects
│   ├── Elementum.ServiceApi/           # REST API
│   │   ├── README.md
│   │   └── API-Endpoints.md
│   ├── Elementum.WorkerService/        # Price ingestion worker
│   │   └── README.md
│   ├── Elementum.Cli/                  # Console CLI
│   │   └── README.md
│   ├── Elementum.Shared/               # DTOs, enums, mapping
│   └── Elementum.Infrastructure/       # DbContext, repositories, resilience
├── tests/                              # Automated unit and integration tests
│   ├── Elementum.ServiceApi.Tests/
│   ├── Elementum.WorkerService.Tests/
│   └── Elementum.Cli.Tests/
├── Elementum.slnx                      # Central .NET solution
└── README.md                           # This file
```

---

## Prerequisites

- **.NET SDK** (e.g. .NET 10) for building and running the API, Worker, and CLI
- **Docker and Docker Compose** for running the database and API in containers (see [docker/README.md](docker/README.md))
- **MySQL** (or the containerized instance) if you run the API or Worker outside Docker

---

## Getting started

### Option A: Docker (database + API)

1. From the repository root:
   ```bash
   cd docker
   cp .env.example .env   # optional: adjust ports and credentials
   docker compose up -d
   ```
2. The API is available at **http://localhost:5000** (or the port defined by `API_PORT` in `.env`).  
   Health endpoints: `GET /health/live`, `GET /health/ready`.
3. Run the CLI (requires the API to be running):
   ```bash
   dotnet run --project src/Elementum.Cli
   ```
   To use a different API base URL, set the environment variable `ELEMENTUM_API_BASEURL` (e.g. `http://localhost:5000/api/v1/`) or configure `ApiBaseUrl` in the CLI’s `appsettings.json`.

For more details (logs, rebuilding after code changes, database-only mode), see [docker/README.md](docker/README.md).

### Option B: Local API and MySQL

1. Start MySQL (e.g. `docker compose up -d mysql` from `docker`) or use an existing instance.
2. Configure the connection string for the API (and Worker) in `appsettings.json` or via environment (e.g. `ConnectionStrings__DefaultConnection`).
3. Run the API:
   ```bash
   dotnet run --project src/Elementum.ServiceApi
   ```
   The API listens on the port configured in the project (e.g. 5000). See [src/Elementum.ServiceApi/README.md](src/Elementum.ServiceApi/README.md).
4. Run the CLI as in Option A and set the API base URL if needed.

To populate the database with daily prices, run the Worker (or install it as a Windows Service). Details: [src/Elementum.WorkerService/README.md](src/Elementum.WorkerService/README.md).

---

## Running the components

| Component | Command / location | Documentation |
|-----------|--------------------|----------------|
| **Database + API (Docker)** | `cd docker && docker compose up -d` | [docker/README.md](docker/README.md) |
| **API (local)** | `dotnet run --project src/Elementum.ServiceApi` | [src/Elementum.ServiceApi/README.md](src/Elementum.ServiceApi/README.md) |
| **API reference** | — | [src/Elementum.ServiceApi/API-Endpoints.md](src/Elementum.ServiceApi/API-Endpoints.md) |
| **Worker** | `dotnet run --project src/Elementum.WorkerService` or install as Windows Service | [src/Elementum.WorkerService/README.md](src/Elementum.WorkerService/README.md) |
| **CLI** | `dotnet run --project src/Elementum.Cli` (API must be running) | [src/Elementum.Cli/README.md](src/Elementum.Cli/README.md) |

---

## Screenshots

### Main menu

*[Screenshot: CLI main menu with options (Dashboard, List metals, Trading, Karat calculator, History, Info).]*

<img width="1008" height="487" alt="image" src="https://github.com/user-attachments/assets/b1d5c4a5-f4fd-4a2e-b925-6cc1038ac9f6" />

### Dashboard

*[Screenshot: Dashboard view with latest prices for all metals.]*

<img width="883" height="425" alt="image" src="https://github.com/user-attachments/assets/1ae8e25b-0cac-494c-8197-5d5f6d8392ce" />

### List metals

*[Screenshot: List metals view (e.g. XAU, XAG, XPT, XPD).]*

<img width="890" height="388" alt="image" src="https://github.com/user-attachments/assets/8e258c89-a9d4-4b1c-a351-064d883e0671" />

### Trading view

*[Screenshot: Trading view for one metal (bid/ask, high/low).]*

<img width="873" height="1033" alt="image" src="https://github.com/user-attachments/assets/b414a7e9-2201-4798-a06b-e1279f1382e4" />

### Karat view

*[Screenshot: Karat view with price per gram by purity.]*

<img width="882" height="719" alt="image" src="https://github.com/user-attachments/assets/f160fae3-de54-40eb-af15-f0c03d87c687" />

### History view

*[Screenshot: History view with price history for a selected metal.]*

<img width="891" height="719" alt="image" src="https://github.com/user-attachments/assets/cd4b6a6c-e794-4e60-a339-f7e126d9d5f5" />

---

## Related documentation

- [src/Elementum.ServiceApi/API-Endpoints.md](src/Elementum.ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [docker/README.md](docker/README.md) — Docker stack (API + DB + worker)
- [docker/init/README.md](docker/init/README.md) — Database init scripts
- [src/Elementum.ServiceApi/README.md](src/Elementum.ServiceApi/README.md) — Running the API locally and with Docker
- [src/Elementum.WorkerService/README.md](src/Elementum.WorkerService/README.md) — Worker setup and Windows Service installation
- [src/Elementum.Cli/README.md](src/Elementum.Cli/README.md) — Command-line client (TUI)
