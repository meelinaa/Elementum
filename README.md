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
| **Elementum-Database** | MySQL schema, init scripts, and a single Docker Compose setup for the database and the API. |
| **Elementum-ServiceApi** | ASP.NET Core REST API: metals catalog, price history (latest, by symbol, date range, aggregated), trading and karat-oriented endpoints. |
| **Elementum-WorkerService** | Background worker that fetches daily metal prices from an external API (e.g. GoldAPI) and persists them to the database. Can be run as a Windows Service. |
| **Elementum_Cli** | Console application that consumes the API and provides a menu-driven UI: dashboard, metal list, trading view, karat calculator, and history. |

The CLI communicates with the API; the API and the Worker share the same MySQL database. The system can be run entirely via Docker (database + API) or with the API and Worker running locally against your own MySQL instance.

---

## Tech stack

- **Runtime & framework:** .NET 10 (C#)
- **API:** ASP.NET Core (REST), health checks (liveness/readiness)
- **Data:** MySQL 8, Entity Framework Core, structured init scripts
- **Infrastructure:** Docker, Docker Compose
- **Client:** Console application (CLI) with configurable API base URL
- **Testing:** Dedicated test projects for API and CLI

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

Data flows from an external price API into the Worker, which writes to MySQL. The ServiceApi reads from the same database and exposes REST endpoints. The CLI calls the API and renders the data in the console. The database schema (metals, price history with OHLC and karat-related fields) is created and seeded via SQL scripts in `Elementum-Database/init/`, which run automatically on first container start.

---

## Repository structure

```
Elementum/
├── Elementum-Database/              # Docker Compose (MySQL + API), init scripts
│   ├── docker-compose.yml
│   ├── .env.example
│   ├── README.md
│   └── init/
│       ├── README.md
│       └── 01-schema.sql
├── Elementum-Services/              # .NET solution (API, Worker, CLI, Shared, Infrastructure)
│   ├── Elementum-ServiceApi/       # REST API
│   │   ├── README.md
│   │   └── API-Endpoints.md
│   ├── Elementum-WorkerService/     # Price ingestion worker
│   │   └── README.md
│   ├── Elementum_Cli/               # Console CLI
│   ├── Elementum.Shared/            # DTOs, enums, mapping
│   └── Elementum.Infrastructure/   # DbContext, repositories
├── docs/
│   └── screenshots/                # CLI screenshots (see Screenshots section)
└── README.md                       # This file
```

---

## Prerequisites

- **.NET SDK** (e.g. .NET 10) for building and running the API, Worker, and CLI
- **Docker and Docker Compose** for running the database and API in containers (see [Elementum-Database/README.md](Elementum-Database/README.md))
- **MySQL** (or the containerized instance) if you run the API or Worker outside Docker

---

## Getting started

### Option A: Docker (database + API)

1. From the repository root:
   ```bash
   cd Elementum-Database
   cp .env.example .env   # optional: adjust ports and credentials
   docker compose up -d
   ```
2. The API is available at **http://localhost:5000** (or the port defined by `API_PORT` in `.env`).  
   Health endpoints: `GET /health/live`, `GET /health/ready`.
3. Run the CLI (requires the API to be running):
   ```bash
   cd Elementum-Services
   dotnet run --project Elementum_Cli
   ```
   To use a different API base URL, set the environment variable `ELEMENTUM_API_BASEURL` (e.g. `http://localhost:5000/api/v1/`) or configure `ApiBaseUrl` in the CLI’s `appsettings.json`.

For more details (logs, rebuilding after code changes, database-only mode), see [Elementum-Database/README.md](Elementum-Database/README.md).

### Option B: Local API and MySQL

1. Start MySQL (e.g. `docker compose up -d mysql` from `Elementum-Database`) or use an existing instance.
2. Configure the connection string for the API (and Worker) in `appsettings.json` or via environment (e.g. `ConnectionStrings__DefaultConnection`).
3. Run the API:
   ```bash
   cd Elementum-Services
   dotnet run --project Elementum-ServiceApi
   ```
   The API listens on the port configured in the project (e.g. 5000). See [Elementum-ServiceApi/README.md](Elementum-Services/Elementum-ServiceApi/README.md).
4. Run the CLI as in Option A and set the API base URL if needed.

To populate the database with daily prices, run the Worker (or install it as a Windows Service). Details: [Elementum-WorkerService/README.md](Elementum-Services/Elementum-WorkerService/README.md).

---

## Running the components

| Component | Command / location | Documentation |
|-----------|--------------------|----------------|
| **Database + API (Docker)** | `cd Elementum-Database && docker compose up -d` | [Elementum-Database/README.md](Elementum-Database/README.md) |
| **API (local)** | `dotnet run --project Elementum-ServiceApi` | [Elementum-ServiceApi/README.md](Elementum-Services/Elementum-ServiceApi/README.md) |
| **API reference** | — | [Elementum-ServiceApi/API-Endpoints.md](Elementum-Services/Elementum-ServiceApi/API-Endpoints.md) |
| **Worker** | `dotnet run --project Elementum-WorkerService` or install as Windows Service | [Elementum-WorkerService/README.md](Elementum-Services/Elementum-WorkerService/README.md) |
| **CLI** | `dotnet run --project Elementum_Cli` (API must be running) | [Elementum_Cli/README.md](Elementum-Services/Elementum_Cli/README.md) |

---

## Screenshots

The CLI offers a main menu and several views. Below are placeholders for screenshots; add your own images to `docs/screenshots/` using the filenames listed in [docs/screenshots/README.md](docs/screenshots/README.md).

### Main menu

*[Screenshot: CLI main menu with options (Dashboard, List metals, Trading, Karat calculator, History, Info).]*

![CLI main menu](docs/screenshots/cli-main-menu.png)

### Dashboard

*[Screenshot: Dashboard view with latest prices for all metals.]*

![CLI dashboard](docs/screenshots/cli-dashboard.png)

### List metals

*[Screenshot: List metals view (e.g. XAU, XAG, XPT, XPD).]*

![CLI list metals](docs/screenshots/cli-list-metals.png)

### Trading view

*[Screenshot: Trading view for one metal (bid/ask, high/low).]*

![CLI trading view](docs/screenshots/cli-trading.png)

### Karat calculator

*[Screenshot: Karat calculator view with price per gram by purity.]*

![CLI karat calculator](docs/screenshots/cli-karat.png)

### History view

*[Screenshot: History view with price history for a selected metal.]*

![CLI history](docs/screenshots/cli-history.png)

---

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](Elementum-Services/Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](Elementum-Database/README.md) — Docker stack (API + DB + worker)
- [Elementum-Database/init/README.md](Elementum-Database/init/README.md) — Database init scripts
- [Elementum-ServiceApi/README.md](Elementum-Services/Elementum-ServiceApi/README.md) — Running the API locally and with Docker
- [Elementum-WorkerService/README.md](Elementum-Services/Elementum-WorkerService/README.md) — Worker setup and Windows Service installation
- [Elementum_Cli/README.md](Elementum-Services/Elementum_Cli/README.md) — Command-line client (TUI)
