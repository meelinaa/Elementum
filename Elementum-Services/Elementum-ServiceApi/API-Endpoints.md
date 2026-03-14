# Elementum-ServiceApi — API Endpoints

Base URL for all API routes: **`/api/v1`**.  
All relevant responses are JSON. The API uses DTOs from `Elementum.Shared.DTOs`.

---

## Health checks

Not part of `/api/v1`; use them for liveness/readiness in Docker or Kubernetes.

| Method | Path | Description |
|--------|------|-------------|
| GET | `/health/live` | **Liveness** — always returns healthy (no dependency checks). |
| GET | `/health/ready` | **Readiness** — checks database connectivity; returns JSON with status and check details. |

Example readiness response (200 when DB is OK):

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

## Metals

### GET `/api/v1/metals/all`

Returns all metals (e.g. XAU, XAG, XPT, XPD).

**Response:** `200 OK` — array of `MetalsDto`.

| Field | Type | Description |
|-------|------|-------------|
| `id` | int | Primary key. |
| `symbol` | string | e.g. `"XAU"`, `"XAG"`. |
| `name` | string | e.g. `"Gold"`, `"Silver"`. |

Example:

```json
[
  { "id": 1, "symbol": "XAU", "name": "Gold" },
  { "id": 2, "symbol": "XAG", "name": "Silver" }
]
```

---

## Price history

### GET `/api/v1/history/all/latest`

Latest price history entry **per metal** (for dashboard-style views).

**Response:** `200 OK` — array of `PriceHistoryDto`.

| Field | Type | Description |
|-------|------|-------------|
| `id` | long | Primary key. |
| `metalId` | int | FK to metals. |
| `currency` | string | e.g. `"USD"`. |
| `exchange` | string? | Optional. |
| `symbol` | string? | Metal symbol. |
| `referenceTimestamp` | long? | Unix-style timestamp. |
| `entryDate` | string | Date (e.g. `"2025-03-14"`). |
| `price` | decimal | Price. |
| `chp` | decimal? | Change in percent. |
| `metal` | object? | Nested `MetalsDto` (id, symbol, name). |

---

### GET `/api/v1/history/{symbol}`

Full price history for **one metal** by symbol (e.g. `XAU`, `XAG`).  
Can return a large payload; consider using date range or aggregated endpoints for big ranges.

**Parameters:**

| Name | In | Type | Description |
|------|-----|------|-------------|
| `symbol` | path | string | Metal symbol (e.g. `XAU`). Required, non-empty. |

**Response:** `200 OK` — array of `PriceHistoryDto`.  
**Validation:** `400` if symbol is missing or invalid.

---

### GET `/api/v1/history/{symbol}/latest`

**Latest** price history entry for one metal.

**Parameters:** `symbol` (path) — metal symbol.

**Response:** `200 OK` — single `PriceHistoryDto`.  
**Not found:** `404` with a problem-details body (e.g. “No price history found for symbol 'XAU'.”).

---

### GET `/api/v1/history/{symbol}/latest/trading`

Latest price for one metal in **trading** form (bid/ask, high/low, open, change). Used by the CLI Trading view.

**Parameters:** `symbol` (path).

**Response:** `200 OK` — single `TradingPriceDto`.

| Field | Type | Description |
|-------|------|-------------|
| `id` | long | Primary key. |
| `symbol` | string | Metal symbol. |
| `metalName` | string | e.g. `"Gold"`. |
| `exchange` | string? | Optional. |
| `currency` | string | e.g. `"USD"`. |
| `entryDate` | string | Date. |
| `referenceTimestamp` | long? | Unix timestamp. |
| `openTime` | long? | Open time. |
| `price` | decimal | Current price. |
| `prevClosePrice` | decimal? | Previous close. |
| `openPrice` | decimal? | Open. |
| `lowPrice` | decimal? | Low. |
| `highPrice` | decimal? | High. |
| `ch` | decimal? | Absolute change. |
| `chp` | decimal? | Change in percent. |
| `ask` | decimal? | Ask price. |
| `bid` | decimal? | Bid price. |

**Not found:** `404` if no price history for that symbol.

---

### GET `/api/v1/history/{symbol}/latest/karat`

Latest price for one metal as **price per gram by purity** (24k down to 10k). Used by the CLI Karat calculator.

**Parameters:** `symbol` (path).

**Response:** `200 OK` — single `KaratPricesDto`.

| Field | Type | Description |
|-------|------|-------------|
| `symbol` | string | Metal symbol. |
| `metalName` | string | e.g. `"Gold"`. |
| `entryDate` | string | Date. |
| `currency` | string | e.g. `"USD"`. |
| `priceGram24k` … `priceGram10k` | decimal? | Price per gram for 24k, 22k, 21k, 20k, 18k, 16k, 14k, 10k. |

**Not found:** `404` if no price history for that symbol.

---

### GET `/api/v1/history/{symbol}/{firstDate}/{lastDate}`

Price history for **one metal** in a **date range**.  
Uses a longer timeout (“DataCruncher”) for large ranges.

**Parameters:**

| Name | In | Type | Format / constraints |
|------|-----|------|------------------------|
| `symbol` | path | string | Metal symbol. |
| `firstDate` | path | string | Start date: `yyyy-MM-dd`. |
| `lastDate` | path | string | End date: `yyyy-MM-dd`; must be ≥ firstDate. |

**Response:** `200 OK` — array of `PriceHistoryDto`.  
**Validation:** `400` if date format is wrong or start &gt; end.

Example: `GET /api/v1/history/XAU/2025-01-01/2025-03-14`

---

### GET `/api/v1/history/{symbol}/aggregated/{aggregation}/{count}`

**Aggregated** price history for one metal.  
Use this for charts or reduced datasets (e.g. last 12 months, last 52 weeks).

**Parameters:**

| Name | In | Type | Description |
|------|-----|------|-------------|
| `symbol` | path | string | Metal symbol. |
| `aggregation` | path | string | One of: `daily`, `weekly`, `monthly`, `yearly`. |
| `count` | path | int | Number of data points (0–500). |

**Response:** `200 OK` — array of `PriceHistoryDto`.  
**Validation:** `400` if aggregation is not one of the four values or count is out of range.

Examples:

- `GET /api/v1/history/XAU/aggregated/monthly/12` — last 12 monthly points for gold.
- `GET /api/v1/history/XAG/aggregated/weekly/52` — last 52 weekly points for silver.

---

## Summary table

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/health/live` | Liveness probe. |
| GET | `/health/ready` | Readiness (DB check). |
| GET | `/api/v1/metals/all` | All metals. |
| GET | `/api/v1/history/all/latest` | Latest per metal (dashboard). |
| GET | `/api/v1/history/{symbol}` | Full history for one metal. |
| GET | `/api/v1/history/{symbol}/latest` | Latest for one metal. |
| GET | `/api/v1/history/{symbol}/latest/trading` | Latest trading (bid/ask, OHLC). |
| GET | `/api/v1/history/{symbol}/latest/karat` | Latest karat (price per gram by purity). |
| GET | `/api/v1/history/{symbol}/{firstDate}/{lastDate}` | History in date range. |
| GET | `/api/v1/history/{symbol}/aggregated/{aggregation}/{count}` | Aggregated history (daily/weekly/monthly/yearly). |

All `history` endpoints that take `symbol` require a non-empty symbol; invalid or missing parameters return `400` with validation details. Missing data for a valid symbol returns `404` where noted.
