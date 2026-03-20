# Elementum CLI

Terminal UI (TUI) for **Elementum**: browse metal prices and history by calling the **Elementum-ServiceApi** REST API. Navigation is keyboard-driven (arrow keys, Enter, Esc).

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (project targets **net10.0**)
- **Service API** reachable — typically `http://localhost:5000` or your Docker-mapped port (see [Elementum-Database/README.md](../../Elementum-Database/README.md))

## Run

From the repository root:

```bash
dotnet run --project Elementum-Services/Elementum_Cli/Elementum_Cli.csproj
```

Or from this folder (`Elementum-Services/Elementum_Cli`):

```bash
dotnet run
```

Ensure the API is running before starting the CLI; otherwise HTTP calls will fail.

## Configuration


| Source                                       | Purpose                                                        |
| -------------------------------------------- | -------------------------------------------------------------- |
| `appsettings.json`                           | `ApiBaseUrl` — default `http://localhost:5000/api/v1/`         |
| Environment variable `ELEMENTUM_API_BASEURL` | Overrides the base URL (must end with a path like `/api/v1/`). |


The CLI loads config from the **application base directory** (next to the built `Elementum_Cli.dll`).

## Main menu (features)


| Item                 | Description                                                                     |
| -------------------- | ------------------------------------------------------------------------------- |
| **Dashboard**        | Market overview: all metals, price and % change                                 |
| **Trading**          | Bid/ask, spreads, volatility, daily comparison (metal-specific)                 |
| **Karat calculator** | Gram prices by purity, alloy discount vs 24k                                    |
| **List metals**      | Master table: id, symbol, name                                                  |
| **History**          | Sparkline (Chp) and USD bar chart by period (daily / weekly / monthly / yearly) |
| **Info**             | GoldAPI policy, daily limits, disclaimer                                        |
| **Exit**             | Quit the application                                                            |


Some views ask you to pick a metal (Gold / Silver / Platinum) first.

## Keyboard


| Key       | Action                                                           |
| --------- | ---------------------------------------------------------------- |
| ↑ / ↓     | Move selection in the main menu                                  |
| Enter / → | Open selected view                                               |
| Esc / ←   | Back (from detail views) or exit from menu                       |
| **R**     | Reload current view (clears in-memory HTTP cache and re-fetches) |


## Screenshots

### Main menu



### Dashboard



### List metals



### Trading view



### Karat calculator



### History



---

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](../Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](../../Elementum-Database/README.md) — Docker stack (API + DB + worker)

