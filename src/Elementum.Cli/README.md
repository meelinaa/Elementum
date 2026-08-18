# Elementum CLI

Interactive Terminal User Interface (TUI) for **Elementum**. Provides real-time dashboard monitoring, technical trading indicators, metal catalog browsing, and historical sparklines by querying the **Elementum.Api** REST endpoints.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Running **Elementum.Api** instance (e.g. `http://localhost:5000` or Docker container)

---

## Running the CLI

From the repository root:

```bash
dotnet run --project src/Elementum.Cli
```

---

## Configuration

| Source | Description |
|--------|-------------|
| `appsettings.json` | `ApiBaseUrl` (e.g. `http://localhost:5000/api/v1/`) |
| Environment Variable | `ELEMENTUM_API_BASEURL` overrides `ApiBaseUrl` |

> [!NOTE]
> The CLI uses **fail-fast configuration resolution** (`CliConfig.ResolveApiBaseUrl`). If neither `appsettings.json` nor the environment variable provides a valid base URL, the CLI throws an `InvalidOperationException` on startup.

---

## Features & Navigation

| Menu Item | Description |
|-----------|-------------|
| **Dashboard** | Real-time market overview with prices in USD and EUR, % changes, and intraday highs/lows. |
| **Trading** | Technical analysis view with OHLC, bid/ask spreads, volatility %, and Bullish/Bearish indicators. |
| **History** | Historical price ticks and trend sparklines. |
| **Info** | External API policies and data disclaimers. |
| **Exit** | Terminate the application. |

---

## Keyboard Controls

| Key | Action |
|-----|--------|
| `↑` / `↓` | Move selection in menu or lists |
| `Enter` / `→` | Open selected view |
| `Esc` / `←` | Return to previous menu / Exit |
| `R` | Reload current view (re-fetches from API) |

---

## Related Documentation

- [API-Endpoints.md](../Elementum.Api/API-Endpoints.md) — REST API routes and data contracts
- [docker/README.md](../../docker/README.md) — Docker Compose deployment
