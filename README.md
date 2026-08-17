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

**Elementum** consists of the following architectural components (Hexagonal / Ports & Adapters):

| Component | Layer | Description |
|-----------|-------|-------------|
| **Elementum.Domain** | Domain | Pure enterprise business models (`Metal`, `PriceHistory`), enums, and driven ports (`IPriceHistoryRepository`, `IMetalsApiClient`, `IDatabaseCheckService`). |
| **Elementum.Application** | Application | Use cases (`IngestPricesUseCase`, `GetPriceHistoryUseCase`, `GetMetalsUseCase`), DTOs, and mappings. |
| **Elementum.Infrastructure** | Infrastructure (Driven Adapters) | EF Core `ElementumDbContext` (MySQL), Polly resilience decorators, GoldAPI HTTP client adapter. |
| **Elementum.Api** | Presentation (Driving Adapter) | ASP.NET Core REST API exposing precious metal prices and catalog endpoints. |
| **Elementum.Worker** | Presentation (Driving Adapter) | Periodic background ingestion worker (schedulable as Windows Service or container). |
| **Elementum.Cli** | Presentation (Driving Adapter) | Console TUI client providing dashboard, metal list, trading view, karat calculator, and history. |
| **docker** | Deployment | Docker Compose environment for MySQL, API, and Worker. |

---

## Tech stack

- **Runtime & framework:** .NET 10 (C#)
- **Architecture:** Hexagonal / Clean Architecture (Ports & Adapters)
- **API:** ASP.NET Core (REST), OpenAPI, health checks (liveness/readiness), per-request timeouts
- **Data:** MySQL 8, Entity Framework Core, structured init scripts
- **Resilience:** Polly (retry policies for MySQL connection drops and external API calls)
- **Infrastructure:** Docker, Docker Compose
- **Client:** Console application (CLI) with configurable API base URL
- **Testing:** Dedicated xUnit & Moq test projects for Domain, Application, Infrastructure, Api, Worker, and CLI

---

## Features

- **REST API** with versioned base path (`/api/v1`), validation, and clear DTOs for metals and price history
- **Flexible queries:** latest per metal, by symbol, by date range, and aggregated (daily/weekly/monthly/yearly)
- **Specialized views:** trading (bid/ask, OHLC) and karat (price per gram by purity) endpoints for the CLI
- **Docker-based deployment:** one Compose file for MySQL, API, and Worker; database init on first run
- **Background ingestion:** worker with configurable schedule; installable as Windows Service
- **Console CLI:** keyboard-driven navigation, dashboard, metal selection, and multiple detail views

---

## Architecture (Hexagonal / Ports & Adapters)

Data flows from an external price API via the `MetalsApiClient` (Driven Adapter) into the `IngestPricesUseCase` (Application), which persists data via `IPriceHistoryRepository` (Driven Port) into MySQL. The `Elementum.Api` (Driving Adapter) invokes application use cases to serve REST requests to `Elementum.Cli`. The database schema is seeded via SQL scripts in `docker/init/`.

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
│   ├── Elementum.Domain/               # Pure domain entities, enums, rules & ports
│   ├── Elementum.Application/          # Use cases, queries, commands, DTOs & mappings
│   ├── Elementum.Infrastructure/       # EF Core DbContext, GoldAPI client, Polly resilience
│   ├── Elementum.Api/                  # ASP.NET Core REST API
│   │   ├── README.md
│   │   └── API-Endpoints.md
│   ├── Elementum.Worker/               # Price ingestion background worker
│   │   └── README.md
│   └── Elementum.Cli/                  # Console CLI
│       └── README.md
├── tests/                              # Automated unit and integration tests
│   ├── Elementum.Application.Tests/
│   ├── Elementum.Api.Tests/
│   ├── Elementum.Worker.Tests/
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
   dotnet run --project src/Elementum.Api
   ```
   The API listens on the port configured in the project (e.g. 5000). See [src/Elementum.Api/README.md](src/Elementum.Api/README.md).
4. Run the CLI as in Option A and set the API base URL if needed.

To populate the database with daily prices, run the Worker (or install it as a Windows Service). Details: [src/Elementum.Worker/README.md](src/Elementum.Worker/README.md).

---

## Running the components

| Component | Command / location | Documentation |
|-----------|--------------------|----------------|
| **Database + API (Docker)** | `cd docker && docker compose up -d` | [docker/README.md](docker/README.md) |
| **API (local)** | `dotnet run --project src/Elementum.Api` | [src/Elementum.Api/README.md](src/Elementum.Api/README.md) |
| **API reference** | — | [src/Elementum.Api/API-Endpoints.md](src/Elementum.Api/API-Endpoints.md) |
| **Worker** | `dotnet run --project src/Elementum.Worker` or install as Windows Service | [src/Elementum.Worker/README.md](src/Elementum.Worker/README.md) |
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
