<div align="center">
  <img width="100%" alt="Elementum Banner" src="https://github.com/user-attachments/assets/ea4edcb2-94d2-41b8-8d60-de1f83949946" />

  # Elementum
  
  **A modern, full-stack .NET application for real-time precious metal price tracking and trading analytics.**

  [![CI/CD Pipeline](https://github.com/meelinaa/Elementum/actions/workflows/ci.yml/badge.svg)](https://github.com/meelinaa/Elementum/actions/workflows/ci.yml)
  ![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
  ![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10.0-512BD4?logo=dotnet)
  ![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql&logoColor=white)
  ![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)
  ![License](https://img.shields.io/badge/License-MIT-green.svg)
</div>

<br />

---

## Table of Contents

- [Problem & Solution](#problem--solution)
- [Project Overview](#project-overview)
- [Tech Stack](#tech-stack)
- [Architecture & Design](#architecture--design)
- [Core Features](#core-features)
- [Repository Structure](#repository-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Screenshots](#screenshots)
- [Related Documentation](#related-documentation)

---

## Problem & Solution

> **Problem:** Tracking precious metal spot prices over time (per metal, currency, and historical trends) usually requires manually aggregating fragmented data sources, dealing with API rate limits, and lacking consolidated technical analytics.
>
> **Solution:** **Elementum** automates periodic background price ingestion, consolidates daily candle data, and exposes real-time market overviews and technical trading indicators through a high-performance REST API and an interactive Terminal UI (CLI) — powered by a shared MySQL database with Polly resilience, HybridCache L1/L2 caching, distributed locking, and IP-based rate limiting.

- **Stack:** .NET 10 (C# 13), ASP.NET Core, MySQL 8, Entity Framework Core, Docker & Docker Compose.
- **Key Capabilities:** Versioned endpoints (`/api/v1`), live market overviews (USD & EUR), real-time technical trading metrics (OHLC, spread, volatility %, bullish/bearish indicators), historical tick queries, fail-fast configuration validation, Kubernetes-style health probes (`/health/live`, `/health/ready`), and 1-command Docker Compose deployment.

---

## Project Overview

**Elementum** is structured into decoupled architectural layers:

| Component | Layer | Description |
|-----------|-------|-------------|
| **Elementum.Domain** | Domain | Pure enterprise business models (`Metals`, `PriceHistory`, `DailyPriceSummary`), value objects (`Currency`, `Money`), domain constants, and ports (`IPriceHistoryRepository`, `IMetalsApiClient`, `IDistributedLockProvider`). |
| **Elementum.Application** | Application | Use cases (`IngestPricesUseCase`, `LivePricesUseCase`, `GetPriceHistoryUseCase`, `GetDailyCandlesUseCase`, `GetMetalsUseCase`), DTOs, mappers, and fail-fast options. |
| **Elementum.Infrastructure** | Infrastructure (Driven Adapters) | EF Core `ElementumDbContext` (MySQL), Polly resilience retry policies, `MetalsApiClient`, HybridCache L1/L2 caching, and distributed locking. |
| **Elementum.Api** | Presentation (Driving Adapter) | ASP.NET Core REST API exposing live prices, trading indicators, and historical data with IP-based Rate Limiting. |
| **Elementum.Worker** | Presentation (Driving Adapter) | Background daemon for periodic price ingestion, candle consolidation, retention cleanup, and Prometheus/OpenTelemetry metrics. |
| **Elementum.Cli** | Presentation (Driving Adapter) | Interactive Console TUI client with real-time dashboard, trading indicators, metal master data, and charts. |
| **docker** | Deployment | Docker Compose environment for MySQL, API, and Worker services. |

---

## Tech Stack

- **Runtime & Framework:** .NET 10 (C# 13)
- **Architecture:** Hexagonal / Clean Architecture (Ports & Adapters)
- **API & Security:** ASP.NET Core (REST), OpenAPI, IP-based Rate Limiting (`Microsoft.AspNetCore.RateLimiting`), RFC 7807 `ProblemDetails`, per-request timeouts
- **Configuration:** Fail-Fast Options Pattern with DataAnnotations validation (`ValidateDataAnnotations().ValidateOnStart()`)
- **Data & Persistence:** MySQL 8, Entity Framework Core, structured initialization & migrations
- **Caching & Concurrency:** Microsoft HybridCache (L1 Memory + L2 Distributed Redis), distributed locking
- **Resilience:** Polly v8 (retry pipelines with exponential backoff & jitter)
- **Observability:** Serilog structured logging with `X-Correlation-ID` tracing, health checks (`/health/live`, `/health/ready`), `System.Diagnostics.Metrics`
- **Testing:** Automated xUnit test suite (Unit Tests, Integration Tests with WebApplicationFactory and Testcontainers)

---

## Architecture & Design

```mermaid
graph TD
    CLI[Elementum.Cli] -->|REST API HTTP| API[Elementum.Api]
    WORKER[Elementum.Worker] -->|Ingestion & Consolidation| APP[Elementum.Application]
    API -->|Use Cases| APP
    APP -->|Domain Models & Ports| DOMAIN[Elementum.Domain]
    INFRA[Elementum.Infrastructure] -->|Implements Ports| DOMAIN
    INFRA -->|EF Core / Polly| DB[(MySQL 8)]
    INFRA -->|HTTP / Resilience| EXT[External Metals API]
```

- **Fail-Fast Configuration:** Zero silent fallbacks. Missing connection strings, invalid URLs, or out-of-range intervals immediately abort host startup with descriptive error messages.
- **Contract Decoupling (DTOs):** Domain and EF entities are never exposed across HTTP boundaries. All responses are projected via `PriceHistoryMapper`.
- **IP-Based Rate Limiting:** Built-in partition-based rate limiter returns `429 Too Many Requests` with RFC 7807 ProblemDetails when limits are exceeded.

---

## Repository Structure

```
Elementum/
├── docker/                             # Docker Compose (MySQL + API + Worker)
│   ├── docker-compose.yml
│   ├── .env.example
│   └── README.md
├── src/                                # Source projects
│   ├── Elementum.Domain/               # Domain entities, value objects, constants & ports
│   ├── Elementum.Application/          # Use cases, DTOs, mappers & configuration options
│   ├── Elementum.Infrastructure/       # EF Core, HybridCache, external API client & Polly resilience
│   ├── Elementum.Api/                  # ASP.NET Core REST API & Rate Limiting
│   │   ├── README.md
│   │   └── API-Endpoints.md
│   ├── Elementum.Worker/               # Background ingestion daemon & metrics
│   │   └── README.md
│   └── Elementum.Cli/                  # Interactive Console CLI (TUI)
│       └── README.md
├── tests/                              # Test suites
│   ├── Elementum.UnitTests/            # Domain, Application, and API unit tests
│   └── Elementum.IntegrationTests/     # E2E API & Rate Limiting integration tests
├── Elementum.slnx                      # Central .NET solution
└── README.md                           # This file
```

---

## Prerequisites

- **.NET 10 SDK** for building and running the projects
- **Docker & Docker Compose** for running containerized MySQL, API, and Worker
- **MySQL 8** (or the containerized instance)

---

## Getting Started

### Option A: Docker (Database + API + Worker)

1. Navigate to the `docker/` directory:
   ```bash
   cd docker
   cp .env.example .env
   docker compose up -d
   ```
2. The API is available at **http://localhost:5000** (or the configured `API_PORT`).
   - Liveness Probe: `GET /health/live`
   - Readiness Probe: `GET /health/ready`
3. Run the CLI:
   ```bash
   dotnet run --project src/Elementum.Cli
   ```

### Option B: Local Development

1. Start MySQL (e.g. `docker compose up -d mysql` from `docker/`).
2. Run the API:
   ```bash
   dotnet run --project src/Elementum.Api
   ```
3. Run the Worker:
   ```bash
   dotnet run --project src/Elementum.Worker
   ```
4. Run the CLI:
   ```bash
   dotnet run --project src/Elementum.Cli
   ```

---

## Running Automated Tests

```bash
# Run all unit and integration tests
dotnet test Elementum.slnx

# Run in Release configuration
dotnet test Elementum.slnx --configuration Release
```

---

## Screenshots

### Main Menu
<img width="1008" height="487" alt="Main Menu" src="https://github.com/user-attachments/assets/b1d5c4a5-f4fd-4a2e-b925-6cc1038ac9f6" />

### Dashboard
<img width="883" height="425" alt="Dashboard" src="https://github.com/user-attachments/assets/1ae8e25b-0cac-494c-8197-5d5f6d8392ce" />

### Trading View
<img width="873" height="1033" alt="Trading View" src="https://github.com/user-attachments/assets/b414a7e9-2201-4798-a06b-e1279f1382e4" />

### History View
<img width="891" height="719" alt="History View" src="https://github.com/user-attachments/assets/cd4b6a6c-e794-4e60-a339-f7e126d9d5f5" />

---

## Related Documentation

- [API-Endpoints.md](src/Elementum.Api/API-Endpoints.md) — Complete REST API route documentation & DTOs
- [Elementum.Api README](src/Elementum.Api/README.md) — API architecture, rate limiting & hosting
- [Elementum.Worker README](src/Elementum.Worker/README.md) — Worker scheduling, daemon mode & metrics
- [Elementum.Cli README](src/Elementum.Cli/README.md) — Console client navigation & keyboard shortcuts
- [Docker README](docker/README.md) — Docker Compose configuration & deployment
