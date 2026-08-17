# Elementum.Worker (Background Ingestion & Single-Run Job)

The **Elementum.Worker** is responsible for fetching daily precious metal spot prices from the external GoldAPI and persisting them into MySQL.

It supports **two operational modes**:

---

## 1. Single-Run Mode (Cron / Windows Aufgabenplanung / Kubernetes CronJob)

Ideal for scheduled batch jobs (0 MB RAM & 0% CPU consumption outside the execution window):

```bash
# Via .NET CLI
dotnet run --project src/Elementum.Worker -- --run-once

# Or with short flag
dotnet run --project src/Elementum.Worker -- --once

# Or with the compiled binary
Elementum.Worker.exe --run-once
```

### Behavior:
1. Bootstraps Dependency Injection, Configuration, and Resilience pipelines.
2. Executes `IIngestPricesUseCase.ExecuteAsync()`.
3. Returns exit code `0` on success (or `1` on error) and terminates immediately.

---

## 2. Daemon Mode (24/7 Service / Docker Container / Windows Service)

Runs continuously in the background and executes the ingestion at the configured schedule (`WorkerSchedule:DailyRunTime`).

```bash
# Start daemon
dotnet run --project src/Elementum.Worker
```

### Features in Daemon Mode:
- **Periodic Scheduling:** Configurable daily run time via `appsettings.json` or `.env`.
- **Health Check Endpoint:** Exposes `http://localhost:5094/health` for orchestrator liveness/readiness probes.
- **Continuous Metrics:** Records metrics via `IngestionMetrics` for production observability.

---

## Windows Service Installation (Optional)

1. **Publish:**
   ```powershell
   dotnet publish src/Elementum.Worker/Elementum.Worker.csproj -c Release -o bin/publish
   ```
2. **Register as Windows Service (Run as Administrator):**
   ```cmd
   sc.exe create "Elementum.Worker" binpath= "C:\path\to\publish\Elementum.Worker.exe" start= auto
   sc.exe description "Elementum.Worker" "Fetches daily metal prices from GoldAPI into Elementum MySQL database."
   ```
3. **Start Service:**
   ```cmd
   sc.exe start "Elementum.Worker"
   ```
