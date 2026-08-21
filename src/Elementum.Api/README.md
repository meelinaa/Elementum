# Elementum.Api

ASP.NET Core **REST API** for **Elementum**. It exposes clean, read-only access to precious metal live prices, technical trading indicators, and historical price ticks stored in MySQL.

---

## Architectural Role

- **Controllers:** Modularity with dedicated controllers (`LivePricesController`, `PriceHistoryController`).
- **Use Cases:** Driven by application interactors (`ILivePricesUseCase`, `IGetPriceHistoryUseCase`).
- **Decoupled Contracts (DTOs):** Exposes strongly-typed DTOs (`LiveMarketOverviewDto`, `TradingPriceDto`, `PriceHistoryDto`) mapped via `PriceHistoryMapper`.
- **Fail-Fast Configuration:** Configuration options (`MetalsApiOptions`, `RateLimitingOptions`) are validated on startup (`ValidateDataAnnotations().ValidateOnStart()`).

---

## Security & Operational Measures

Authentication is **intentionally omitted**. Elementum.Api is a public, read-only market-data API: GET endpoints for precious-metal spot prices and derived trading indicators. There are no user accounts, no personal data, and no write operations. The upstream vendor (`api.edelmetalle.de`) publishes the same quotes without auth. Public weather and FX APIs often use the same model: **rate limiting instead of identity**. IP-based rate limiting (100 req/min, `429` ProblemDetails) is the relevant control here — it bounds abuse and overload, it does not gate who may *see* public prices.

| Feature | Description |
|---------|-------------|
| **No authentication (by design)** | Public GET-only surface; see above. |
| **IP-Based Rate Limiting** | Native `Microsoft.AspNetCore.RateLimiting` partitioned per client IP address. Exceeded limits return `429 Too Many Requests` with RFC 7807 `ProblemDetails`. |
| **Fail-Fast Options** | Startup validation prevents the application from booting if critical configuration (connection strings, API URLs) is missing or invalid. |
| **CORS** | Development: `AllowAnyOrigin`. Production (`FrontendPolicy`): `Cors:AllowedOrigins` from config / Compose `CORS_ALLOWED_ORIGIN` (HTTPS frontend origin). |
| **HTTPS** | TLS terminates at the reverse proxy (Compose profile `edge`, Caddy). API containers stay HTTP on the Docker network and honor `X-Forwarded-Proto`. |
| **RFC 7807 Problem Details** | Standardized JSON error format (`application/problem+json`) for validation errors, 404s, 429s, and 500s. |
| **Per-Request Timeouts** | Configured timeout policies (`Strict` = 5s, `Default` = 30s, `DataCruncher` = 60s); returns `504 Gateway Timeout` when exceeded. |
| **Structured Logging & Tracing** | Serilog logging with `X-Correlation-ID` enrichment for end-to-end request tracing. |
| **Health Checks** | `/health/live` (process liveness) and `/health/ready` (database connectivity). |
| **OpenAPI** | Native OpenAPI generation in Development mode via `.MapOpenApi()`. |

---

## Configuration (`appsettings.Development.json`)

Copy `appsettings.Development.json.example` to `appsettings.Development.json` (gitignored, like `.env`) and replace the placeholders:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3307;Database=Elementum-Database;User=<MYSQL_USER>;Password=<MYSQL_PASSWORD>;"
  },
  "MetalsApi": {
    "BaseUrl": "https://api.edelmetalle.de/public.json",
    "Currency": "USD"
  },
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowSeconds": 60,
    "QueueLimit": 0
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:3000" ]
  }
}
```

---

## Running Locally

```bash
# Run API directly
dotnet run --project src/Elementum.Api
```

The API starts on configured ports (e.g. `http://localhost:5000` or `https://localhost:5001`).

---

## Related Documentation

- [API-Endpoints.md](API-Endpoints.md) — Complete REST route and DTO documentation
- [docker/README.md](../../docker/README.md) — Containerized deployment with Docker Compose
