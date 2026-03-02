# API To-Dos & Roadmap

A prioritized list of improvements and features for the Elementum Service API, from easier to more advanced. Each item explains what it is and why it matters.

---

## 1.

### CORS configuration ✅

**What:** Configure Cross-Origin Resource Sharing so that the frontend (running on another origin, e.g. `https://myapp.com` or `http://localhost:3000`) is allowed to call this API. Without it, browsers block requests from different origins.

**Why:** The API will be consumed by a web frontend; CORS is required for that to work in the browser.

---

### Health checks ✅

**What:** Add endpoints like `GET /health` or `GET /ready` that return the API status and optionally the database connection status (e.g. “healthy” / “degraded”). ASP.NET Core has built-in health check middleware.

**Why:** Load balancers, containers, and monitoring tools can ping these endpoints to know if the API is up and if the database is reachable. It also helps during deployment and debugging.

---

### Consistent error responses (e.g. ProblemDetails) ✅

**What:** When something goes wrong (validation error, not found, server error), return a uniform JSON shape (e.g. RFC 7807 Problem Details) with fields like `type`, `title`, `status`, `detail`, and optional `errors` for validation. Use exception handling middleware so raw exceptions never leak to the client.

**Why:** The frontend can handle errors in one place and show clear messages. It also looks professional and is easier to document.

---

### Request validation ✅

**What:** Validate incoming request data (query params, route params, body) using DataAnnotations or FluentValidation. For example: date format and range for `firstDate`/`lastDate`, symbol allowed values, page size limits for pagination.

**Why:** Invalid input is rejected early with a clear 400 response instead of causing errors or wrong data later. It improves security and UX.

---

### DTOs instead of entities in responses

**What:** Introduce response models (e.g. `MetalDto`, `PriceHistoryDto`) and map from domain/EF entities to these DTOs before returning. The API contract is then independent of the database schema.

**Why:** You can change the database or add internal fields without breaking the API. You can also shape responses for the frontend (e.g. hide IDs, add computed fields, flatten relations) in one place.

---

## 2.

### Aggregation

**Aggregation / downsampling**: for **charts**. A chart needs one continuous line from start to end; it does **not** want “only the first 50 rows”. Sending thousands of daily points (e.g. 10 years × 365 ≈ 3 650 points) makes the payload huge and the chart slow or “muddy”. The solution is to **aggregate** by time interval so the API returns a small, fixed number of points (e.g. 50–200) that still represent the full range.

#### Aggregation (downsampling) for chart endpoints

**What:** For history endpoints used by charts, support an **interval** or **granularity** (e.g. query param `interval=daily|weekly|monthly|quarterly`). Serverside: group by that interval and return one value per bucket (e.g. average price per month). Aim for roughly **50–200 data points** per response: too few and the line looks jagged; too many and the browser struggles and the trend is hard to see.

**Suggested mapping (time range → resolution):**

| Time range   | Resolution (granularity)   | Approx. points in chart |
|--------------|----------------------------|--------------------------|
| 7 days       | 1 value per day            | 7                        |
| 1 month      | 1 value per day            | ~30                      |
| 1 year       | 1 value per week (e.g. avg)| ~52                      |
| 5 years      | 1 value per month (e.g. avg)| ~60                     |
| All time     | 1 value per quarter or year| variable                 |

**How:** Extend the API with something like `interval` or `granularity`. In the service/repository, filter by date range and symbol as now, then:

- If `interval=daily`: return one row per day (no grouping).
- If `interval=monthly`: `GroupBy(Year, Month)`, return e.g. `Date = first of month`, `Price = Average(Price)` (or open/close if you prefer), ordered by date.
- Same idea for weekly, quarterly, etc.

Optional improvement: **auto-interval**. Derive the interval from the requested date range (e.g. if `lastDate - firstDate > 2 years` then use monthly) so the frontend can send only `firstDate` and `lastDate` and still get a good number of points. (Similar to TradingView / Yahoo Finance.)

**Why:** Charts need the full time range in one response, but with a bounded number of points. Aggregation keeps the API and the frontend fast and the chart readable.

---

### “Latest” / current price per metal ✅

**What:** An endpoint that returns the most recent price (or latest row in `price_history`) per metal, e.g. `GET /api/metals/current` or include “latest price” in the metals list. Optionally add a simple day-over-day or week-over-week change.

**Why:** The frontend needs a single call to show “current” prices and small trend indicators without fetching full history.

---

### API versioning ✅

**What:** Put the API under a version prefix, e.g. `/api/v1/metals`. When you introduce breaking changes later, you add `/api/v2/...` and can keep v1 for a while.

**Why:** You can evolve the API without breaking existing clients. Common practice for any API that will be used by more than one consumer or over time.

---

### OpenAPI (Swagger) documentation

**What:** Expose and maintain the OpenAPI spec (e.g. via Swagger UI in development). Add descriptions, examples, and response codes to the main endpoints. If you add auth later, document it in the spec.

**Why:** Frontend developers (and you) can see all endpoints, parameters, and response shapes in one place. Tools can generate clients or test requests from the spec.

---

### Service interfaces (e.g. IApiService) ✅

**What:** Define interfaces for the service layer (e.g. `IMetalsService`, `IPriceHistoryService` or a single `IApiService`) and register the implementation in DI. Controllers depend on the interface, not the concrete class.

**Why:** Easier unit testing (mock the interface) and clearer architecture. It’s a standard mid-level practice.

---

### Caching for read-heavy data

**What:** Cache responses that change rarely (e.g. list of metals, or “latest price per metal”) using in-memory cache or response caching. Set a short TTL (e.g. 1–5 minutes) so data is still reasonably fresh.

**Why:** Reduces database load and improves response time for frequently called endpoints. Especially useful once the frontend polls or many users hit the same data.

---

## 3.

### Authentication and authorization

**What:** Protect (all or some) endpoints with authentication (e.g. JWT bearer tokens or API keys). Optionally add roles (e.g. “read-only” vs “admin”) and check them for write or admin-only endpoints.

**Why:** If the API is not public, you need to know who is calling and what they are allowed to do. Required for user-specific features (e.g. favorites) or admin operations.

---

### Rate limiting

**What:** Limit how many requests a client (by IP or by API key) can make per minute/hour. Return 429 Too Many Requests when the limit is exceeded.

**Why:** Prevents abuse and protects the API and database from overload. Good practice for any API that might be called by many clients or automated scripts.

---

### Structured logging and correlation IDs

**What:** Log important operations (request start/end, errors, maybe key queries) in a structured format (e.g. JSON). Add a correlation ID per request (in a header or response) and include it in every log line for that request.

**Why:** In production you can trace a single request across logs and quickly find what went wrong. Standard practice for mid/senior-level APIs.

---

### Analytics / derived endpoints for the frontend

**What:** Add endpoints that return computed data the frontend can display directly:

- **Stats per metal over a period:** min, max, average (and optionally median) price in a date range, e.g. `GET /api/analytics/metals/{symbol}/stats?from=...&to=...`.
- **Performance / return:** e.g. percentage change over a period (“Gold +5.2% last month”), e.g. `GET /api/analytics/metals/{symbol}/performance?from=...&to=...`.
- **Dashboard snapshot:** one endpoint that returns all metals with latest price and a simple change indicator (e.g. vs. previous day or week), so the frontend can render an overview with a single call.
- **Gram prices by purity:** expose the stored per-gram prices (24k, 22k, …) in a clear format for the frontend (e.g. for a “price per gram by purity” view or calculator).
- **Simple trend:** e.g. “up” / “down” / “sideways” based on the last N points or first vs. last price in a range, for badges or labels in the UI.

**Why:** The frontend can show charts, dashboards, and comparisons without doing heavy calculations client-side. It keeps the API as the single place for business logic and analytics.

---

### Optional repository layer

**What:** Introduce a thin repository (e.g. `IMetalsRepository`, `IPriceHistoryRepository`) that wraps DbContext access. The service layer calls the repository; the repository uses the DbContext. You can keep query logic in the repository or in the DbContext, but the boundary is clear.

**Why:** Cleaner separation of concerns and easier to test or swap data access later. Useful as the codebase grows.

---

## 4. Feature Ideas for the Frontend (Later)

- **Multi-metal comparison:** e.g. normalized series (e.g. “all metals = 100 on date X”) for chart comparison.
- **Favorites / watchlist:** (after auth) user saves a list of symbols; one endpoint returns only those metals with latest price and optional mini-stats.
- **Alerts / notifications:** (later) user defines thresholds (e.g. “Gold > 2000”); a background job checks and triggers events; frontend shows an “Alerts” section.
