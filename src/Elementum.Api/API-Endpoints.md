# Elementum.Api — REST API Endpoints

Base URL for all API routes: **`/api/v1`**.  
All business responses are JSON and mapped to dedicated DTOs from `Elementum.Application.DTOs`.  
The API is **public and read-only** (no authentication). Protection against overload is IP rate limiting — see [Rate Limiting & Error Responses](#rate-limiting--error-responses).

---

## Health Checks

Liveness and readiness probes for Docker, Kubernetes, and orchestrators (outside `/api/v1`).

| Method | Path | Purpose | Description |
|--------|------|---------|-------------|
| GET | `/health/live` | **Liveness Probe** | Always returns `200 OK` (process is alive). |
| GET | `/health/ready` | **Readiness Probe** | Verifies database connectivity; returns component health status. |

**Example Readiness Response (`200 OK`):**
```json
{
  "Status": "Healthy",
  "Checks": [
    {
      "Component": "database",
      "Status": "Healthy",
      "Description": null
    }
  ],
  "Duration": "00:00:00.0123456"
}
```

---

## Live Prices & Trading Endpoints (`LivePricesController`)

### 1. GET `/api/v1/prices/live`

Retrieves a real-time market overview for all 4 precious metals (Gold, Silver, Platinum, Palladium) in USD and EUR.

- **Response:** `200 OK` → `LiveMarketOverviewDto`

**Payload Schema:**
```json
{
  "items": [
    {
      "symbol": "XAU",
      "name": "Gold",
      "priceUsd": 2500.50,
      "priceEur": 2280.30,
      "openPriceUsd": 2485.00,
      "openPriceEur": 2270.00,
      "chpUsd": 0.62,
      "chpEur": 0.45,
      "highPriceUsd": 2510.00,
      "highPriceEur": 2290.00,
      "lowPriceUsd": 2475.00,
      "lowPriceEur": 2260.00,
      "prevCloseUsd": 2480.00,
      "prevCloseEur": 2265.00
    }
  ],
  "exchangeRateUsdEur": 1.1575,
  "timestamp": 1723900000,
  "lastUpdatedAtLocal": "2026-08-18T14:30:00"
}
```

---

### 2. GET `/api/v1/prices/live/trading/{symbol}?currency={currency}`

Calculates real-time trading metrics and technical indicators (OHLC, spread, volatility, bullish/bearish status) for a given metal.

- **Route Parameter:** `symbol` (e.g. `XAU`, `XAG`, `XPT`, `XPD`)
- **Query Parameter:** `currency` (optional: `EUR` [default] or `USD`; other values return `400`)
- **Response:**
  - `200 OK` → `TradingPriceDto`
  - `404 Not Found` → `ProblemDetails` with type `https://tools.ietf.org/html/rfc7231#section-6.5.4` (same mapper as the exception path)

**Payload Schema:**
```json
{
  "id": 0,
  "symbol": "XAU",
  "metalName": "Gold",
  "exchange": "EDELMETALLE",
  "currency": "EUR",
  "entryDate": "2026-08-18",
  "referenceTimestamp": 1723900000,
  "price": 2280.30,
  "prevClosePrice": 2265.00,
  "openPrice": 2270.00,
  "lowPrice": 2260.00,
  "highPrice": 2290.00,
  "ch": 10.30,
  "chp": 0.45,
  "differencePrevClose": 15.30,
  "volatilityRange": 30.00,
  "volatilityPercent": 1.33,
  "status": "BULLISH ▲",
  "exchangeRateUsdEur": 1.1575
}
```

---

## Price History Endpoints (`PriceHistoryController`)

### 3. GET `/api/v1/history/{symbol}`

Returns a **bounded** historical tick page for a metal. Unbounded dumps are rejected by a date window plus a hard `take` cap.

- **Route Parameter:** `symbol` (e.g. `XAU`, `XAG`)
- **Query Parameters:**
  - `currency` (optional: `USD` or `EUR`; omitted = all currencies; other values return `400`)
  - `from` / `to` (optional, ISO `yyyy-MM-dd`). Omitted = last 30 UTC days. `to` cannot be in the future; `from` cannot be after `to`.
  - `skip` (optional, default `0`, must be ≥ 0)
  - `take` (optional, default `500`). Values above **2000** are **clamped** to 2000 (HTTP 200, not 400).
- **Timeout Policy:** `DataCruncher` (60s)
- **Response:** `200 OK` → `PriceHistoryPageDto`

**Example:** `GET /api/v1/history/XAU?from=2026-01-01&to=2026-02-01&take=500&skip=0`

**Payload Schema:**
```json
{
  "items": [
    {
      "id": 42,
      "metalId": 1,
      "currency": "USD",
      "symbol": "XAUUSD",
      "referenceTimestamp": "1723900000",
      "entryDate": "2026-08-18",
      "price": 2500.50,
      "chp": 0.62,
      "metal": {
        "id": 1,
        "symbol": "XAU",
        "name": "Gold"
      }
    }
  ],
  "skip": 0,
  "take": 500,
  "totalCount": 1,
  "hasMore": false,
  "from": "2026-07-22",
  "to": "2026-08-21"
}
```

---

## Rate Limiting & Error Responses

### HTTP 429 Too Many Requests
If a client IP exceeds the configured request limit (e.g. >100 req / min), the API immediately responds with status `429` and an RFC 7807 `ProblemDetails` document:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.20",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Please try again later.",
  "instance": "GET /api/v1/prices/live"
}
```

### HTTP 400 Validation Error
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "symbol": ["The Symbol field is required."]
  }
}
```

---

## Summary Table

| Method | Route | Controller | Description |
|--------|-------|------------|-------------|
| GET | `/health/live` | HealthCheck | Liveness probe (process-up). |
| GET | `/health/ready` | HealthCheck | Readiness probe (database connectivity). |
| GET | `/api/v1/prices/live` | `LivePricesController` | Real-time market overview for all metals in USD & EUR. |
| GET | `/api/v1/prices/live/trading/{symbol}` | `LivePricesController` | Live trading analysis & technical indicators. |
| GET | `/api/v1/history/{symbol}` | `PriceHistoryController` | Paged historical ticks (`from`/`to`, `skip`/`take`). |
