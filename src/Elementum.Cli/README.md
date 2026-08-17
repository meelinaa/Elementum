# Elementum CLI

Terminal UI (TUI) for **Elementum**: browse metal prices and history by calling the **Elementum-ServiceApi** REST API. Navigation is keyboard-driven (arrow keys, Enter, Esc).

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (project targets **net10.0**)
- **Service API** reachable — typically `http://localhost:5000` or your Docker-mapped port (see [Elementum-Database/README.md](../../Elementum-Database/README.md))

## Run

From the repository root:

```bash
dotnet run --project Elementum-Services/Elementum.Cli/Elementum.Cli.csproj
```

Or from this folder (`Elementum-Services/Elementum.Cli`):

```bash
dotnet run
```

Ensure the API is running before starting the CLI; otherwise HTTP calls will fail.

## Configuration


| Source                                       | Purpose                                                        |
| -------------------------------------------- | -------------------------------------------------------------- |
| `appsettings.json`                           | `ApiBaseUrl` — default `http://localhost:5000/api/v1/`         |
| Environment variable `ELEMENTUM_API_BASEURL` | Overrides the base URL (must end with a path like `/api/v1/`). |


The CLI loads config from the **application base directory** (next to the built `Elementum.Cli.dll`).

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

*[Screenshot: CLI main menu with options (Dashboard, List metals, Trading, Karat calculator, History, Info).]*

<img width="1008" height="487" alt="image" src="https://github.com/user-attachments/assets/b1d5c4a5-f4fd-4a2e-b925-6cc1038ac9f6" />

### Dashboard

*[Screenshot: Dashboard view with latest prices for all metals.]*

<img width="883" height="425" alt="image" src="https://github.com/user-attachments/assets/1ae8e25b-0cac-494c-8197-5d5f6d8392ce" />

### List metals

*[Screenshot: List metals view (e.g. XAU, XAG, XPT, XPD).]*

<img width="890" height="388" alt="image" src="https://github.com/user-attachments/assets/8e258c89-a9d4-4b1c-a351-064d883e0671" />

### Trading view

*[Screenshot: Trading view for one metal (bid/ask, high/low).]*

<img width="873" height="1033" alt="image" src="https://github.com/user-attachments/assets/b414a7e9-2201-4798-a06b-e1279f1382e4" />

### Karat view

*[Screenshot: Karat view with price per gram by purity.]*

<img width="882" height="719" alt="image" src="https://github.com/user-attachments/assets/f160fae3-de54-40eb-af15-f0c03d87c687" />

### History view

*[Screenshot: History view with price history for a selected metal.]*

<img width="891" height="719" alt="image" src="https://github.com/user-attachments/assets/cd4b6a6c-e794-4e60-a339-f7e126d9d5f5" />


---

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](../Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](../../Elementum-Database/README.md) — Docker stack (API + DB + worker)

