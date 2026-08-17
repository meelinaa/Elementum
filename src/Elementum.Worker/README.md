# Elementum Worker as a Windows Service

**Default deployment:** The worker is normally run as a **Docker container** together with MySQL and the API — see `**Elementum-Database/README.md`** and the `worker` service in `Elementum-Database/docker-compose.yml`.

**This page** describes an **alternative**: installing the same worker on a Windows machine as a **Windows Service** (no Docker). Use this when you need a native Windows service instead of the container.

---

The worker can be installed as a Windows service and will then run automatically in the background. The daily ingestion time is configured under `**Worker:DailyRunTime`** in `appsettings.json` (default `23:00:00` local time).

## Prerequisites

- .NET (net10.0) on the machine, or use a self-contained publish (see below).
- **All `sc.exe` commands must be run from an elevated Command Prompt or PowerShell** (right-click → “Run as administrator”).

## 1. Publish the project

From the solution root (or project folder) run:

```powershell
dotnet publish Elementum-WorkerService\Elementum.WorkerService.csproj -c Release -r win-x64 --self-contained true -o Elementum-WorkerService\bin\Release\net10.0\publish\win-x64
```

Alternatively, without `-r win-x64` (framework-dependent; requires .NET runtime installed):

```powershell
dotnet publish Elementum-WorkerService\Elementum.WorkerService.csproj -c Release -o Elementum-WorkerService\bin\publish
```

The EXE will be at `Elementum-WorkerService\bin\publish\Elementum.WorkerService.exe` (or under the self-contained path above).

## 2. Install the service (one-time)

Run **as Administrator**. `binpath=` must be the **full path to the EXE** (use quotes if the path contains spaces).

Self-contained (win-x64):

```cmd
sc.exe create "Elementum-WorkerService" binpath= "C:\Users\..\Elementum.WorkerService.exe" start= auto
```

Optional description:

```cmd
sc.exe description "Elementum-WorkerService" "Fetches daily metal prices from GoldAPI and saves them to the Elementum database."
```

## 3. Start / stop the service

```cmd
sc.exe start "Elementum-WorkerService"
sc.exe stop "Elementum-WorkerService"
```

## 4. Remove the service (uninstall)

Stop the service first, then delete it:

```cmd
sc.exe stop "Elementum-WorkerService"
sc.exe delete "Elementum-WorkerService"
```

## Notes

- **start= auto** makes the service start automatically when Windows starts.
- Configuration (connection string, API key, `**Worker:DailyRunTime`**) is read from `appsettings.json` and `.env` in the **same folder as the EXE** (the publish output). You can override the schedule with the environment variable `**Worker__DailyRunTime`** (e.g. `02:30:00`).
- Logs may appear in Windows Event Viewer under “Application and Services Logs” (if configured), or only in the console when run manually; for proper service logging, consider configuring Serilog to write to a file.

---

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](../Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](../../Elementum-Database/README.md) — Docker stack (API + DB + worker)

