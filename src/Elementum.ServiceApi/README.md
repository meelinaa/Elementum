# Elementum-ServiceApi

ASP.NET Core **REST API** for **Elementum**. It exposes **read-only** access to metal master data and **price history** (daily OHLC, trading fields, karat-based gram prices) stored in MySQL. The **Elementum-WorkerService** ingests prices from an external provider; this API does **not** call third-party price APIs—it only reads what is already in the database.

**Typical consumers:** the **Elementum CLI**, future web or mobile clients, and automation (scripts calling `curl` or HTTP clients). All business responses are **JSON**; routes are versioned under **`/api/v1`**.

---

## What the API does

| Area | Role |
|------|------|
| **Metals** | List catalog entries (id, symbol, name), e.g. XAU / Gold. |
| **Price history** | Latest row per metal, full series by symbol, date ranges, aggregated series (daily / weekly / monthly / yearly), plus specialized “latest” shapes for **trading** (bid/ask, OHLC) and **karat** (price per gram by purity). |
| **Health** | Liveness and readiness probes for Docker/Kubernetes (`/health/live`, `/health/ready`). |

For a **complete route list**, parameter rules, and DTO field tables, see **[API-Endpoints.md](API-Endpoints.md)**.

---

## Security and operational measures

The API is built for **trusted networks** (local dev, Docker internal network, or a private subnet). There is **no built-in API key or JWT** in this project—if you expose the service on the public internet, place it behind a **reverse proxy** (TLS termination, auth, rate limiting) or extend the app with authentication.

| Measure | What it does |
|---------|----------------|
| **HTTPS (non-Development)** | `UseHttpsRedirection()` in production; use TLS at the reverse proxy in Docker. |
| **CORS** | **Development:** permissive CORS for local tooling. **Production:** `Cors:AllowedOrigins` in configuration (default includes `http://localhost:3000`); only listed origins may call the API from browsers. |
| **Errors** | **RFC 7807 Problem Details** (`application/problem+json`) for validation failures; unhandled exceptions mapped to Problem Details; **exception detail text is hidden outside Development** to avoid leaking internals. |
| **Input validation** | Route and query parameters validated; invalid symbols, dates, or aggregation values return **400** with validation details. |
| **Request timeouts** | Default **30 s** per request; stricter/longer named policies exist for future use; timeouts return **504** with a small JSON body. |
| **Logging** | **Serilog** structured logging; **HTTP request logging**; **X-Correlation-ID** middleware for tracing requests across logs. |
| **Database** | EF Core with **resilience** (transient retries) via shared infrastructure; readiness health check fails if the database is unreachable. |
| **OpenAPI** | In **Development**, the OpenAPI document is mapped (`MapOpenApi()`) for tooling and contract inspection. |

---

## How to call the API

**Base path:** `https://<host>:<port>/api/v1` (HTTP in local dev if TLS is not configured).

Use **GET** only for the routes described in [API-Endpoints.md](API-Endpoints.md). Examples (adjust host/port; Docker often maps **8080** or **5000**):

```bash
# List metals
curl -s "http://localhost:5000/api/v1/metals/all"

# Latest price row for gold
curl -s "http://localhost:5000/api/v1/history/XAU/latest"

# Aggregated history (e.g. last 31 daily points)
curl -s "http://localhost:5000/api/v1/history/XAU/aggregated/daily/31"

# Readiness (database check)
curl -s "http://localhost:5000/health/ready"
```

Send header **`Accept: application/json`** if your client defaults to something else (responses are JSON).

---

## What responses look like

**Success:** HTTP **200** with a JSON body (object or array). DTOs are defined in `Elementum.Shared.DTOs`.

**Example — `GET /api/v1/metals/all`**

```json
[
  { "id": 1, "symbol": "XAU", "name": "Gold" },
  { "id": 2, "symbol": "XAG", "name": "Silver" }
]
```

**Example — `GET /api/v1/history/XAU/latest` (truncated; see API-Endpoints.md for all fields)**

```json
{
  "id": 42,
  "metalId": 1,
  "currency": "USD",
  "symbol": "XAU",
  "entryDate": "2026-03-19",
  "price": 5090.00,
  "chp": 0.15,
  "metal": { "id": 1, "symbol": "XAU", "name": "Gold" }
}
```

**Client errors:** **400** validation problems (JSON Problem Details), **404** when no data exists for a valid symbol (where documented).

**Server errors:** **500** with Problem Details; **504** if the request timeout fires.

---

## Run locally

- Set the connection string in `appsettings.json` or via environment variable `CONNECTION_STRING` / `ConnectionStrings__DefaultConnection`.
- From the project folder: `dotnet run`.

## Docker / Docker Compose

A **single** setup for MySQL, API, and worker lives under **Elementum-Database**:

→ **[Elementum-Database/README.md](../../Elementum-Database/README.md)** — run `docker compose up -d` there.

The **Dockerfile** in this project is referenced by that Compose file.

---

## Related documentation

- [API-Endpoints.md](API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](../../Elementum-Database/README.md) — Docker stack (API + DB + worker)
