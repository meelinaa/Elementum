# Elementum Service — Precious Metals Data Pipeline

This service is part of the **Elementum** monorepo. It ingests daily precious metals data (e.g. gold, silver) from an external API, stores it in a database, and exposes it via a REST API for consumption by the frontend.

---

## Overview

The solution is built from three main components:

1. **Worker** — Runs on a schedule (e.g. once per day), calls an external metals API, and persists the results in the database.
2. **API Service** — Reads from the database, performs SQL-based analytics (e.g. 5-day averages), and exposes HTTP endpoints for the frontend.
3. **Frontend (React)** — Talks only to this API (not the external metals API). Shows charts, analytics, and optional forecasts; users can switch the UI language (German/English).

---

## Phase 1: Foundation & Database

- [ ] **Choose and set up the database** (e.g. PostgreSQL, SQL Server, SQLite).
- [ ] **Store the connection string** in configuration (environment variable or `appsettings`); never in source code.
- [ ] **Design tables/schema** for metals data (e.g. date, metal/asset, price, currency, optional source).
- [ ] **Create and run migrations** (e.g. with Entity Framework or similar).
- [ ] **Verify locally:** Start the database, insert and query a row manually.

---

## Phase 2: Worker — Daily API Fetch & Persistence

- [ ] **Create the worker project** (e.g. Worker Service, console app with timer, or Azure Function with timer trigger).
- [ ] **Select an external API** for gold/silver/metals and obtain documentation and API keys.
- [ ] **Configure an HTTP client** (e.g. `HttpClient` with base URL, timeout, and API key header if required).
- [ ] **Define models/DTOs** for the API response (JSON → C# classes).
- [ ] **Implement the external API call** (once per run).
- [ ] **Implement database access** in the worker (read/write metals data).
- [ ] **Schedule daily execution:**
  - Option A: Timer/cron in code (e.g. once per day at 08:00).
  - Option B: External scheduler (Windows Task Scheduler, cron, Kubernetes CronJob, Azure Function timer).
- [ ] **Error handling:** Log API and database failures; avoid unhandled exceptions.
- [ ] **Idempotency:** Prevent duplicate entries for the same day/asset (e.g. upsert or “insert if not exists”).
- [ ] **Configuration:** API URL, API key, and DB connection must come from configuration or environment; never hardcoded.

---

## Phase 3: API Service — Queries & Analytics for the Frontend

- [ ] **Create the API project** (e.g. ASP.NET Core Web API).
- [ ] **Connect to the database** (same connection string as the worker; read-only or query-only access is sufficient).
- [ ] **Expose base endpoints** for raw data, e.g.:
  - `GET /api/metals` or `GET /api/metals?from=...&to=...`
  - Optional: filter by metal (gold/silver).
- [ ] **Implement analytics in SQL** (or via ORM), e.g.:
  - 5-day average per metal or overall.
  - Min/max over a date range.
  - Simple trend/aggregation queries.
- [ ] **Expose analytics as API endpoints**, e.g.:
  - `GET /api/metals/average?days=5`
  - `GET /api/metals/stats?from=...&to=...`
- [ ] **Configure CORS** so the React frontend can call the API.
- [ ] **Document the API** (e.g. Swagger/OpenAPI) to simplify frontend integration.
- [ ] **Use consistent error handling and response shapes** (e.g. JSON with `data`/`error`).

---

## Phase 4: Frontend (React) — UI, Charts, Analytics & Language

- [ ] **Create the React app** (e.g. Vite + React, or Create React App) and ensure it calls only the **own API** (no direct calls to the external metals API).
- [ ] **Internationalisation (i18n):** Let the user choose the UI language (German or English). Store the preference (e.g. localStorage or context) and load the correct translations for labels, tooltips, and messages.
- [ ] **Charts:** Display metals data (e.g. price over time) in interactive charts (e.g. Chart.js, Recharts, or similar). Support zoom, range selection, and tooltips.
- [ ] **Analytics view:** Show analytics from the API (e.g. 5-day average, min/max, stats) in clear sections or cards. Allow filtering by metal and date range where the API supports it.
- [ ] **Interactivity:** Users can change date ranges, switch metals, and interact with charts (hover, zoom, etc.) to explore the data.
- [ ] **(Optional) Forecasts / predictions:** If desired, add a section for simple forecasts (e.g. trend-based or from a dedicated API/backend endpoint) and display them in the UI (e.g. separate chart or highlighted range).

---

## Architecture (High Level)

```
[External Metals API]  →  [Worker: 1× daily]  →  [Database]
                                                       ↓
[React Frontend]  ←  [API Service: queries + SQL analytics]
```

- **Worker:** No HTTP server; runs on a schedule and writes to the database.
- **API Service:** HTTP API with database access, consumed by the frontend.
- **Frontend:** React app; calls only this API. Charts, analytics, optional forecasts; language switch (DE/EN).

---

## Getting Started & Conventions

- **External APIs:** Precious metals APIs often require an API key and have rate limits; check their docs before integration.
- **Deployment:** Worker and API can live in the same solution/repo but can be deployed independently.
- **Secrets:** Do not commit API keys or connection strings. Use environment variables or a secrets manager (see [Project Guidelines](../PROJECT-GUIDELINES.md) in the repo root).
