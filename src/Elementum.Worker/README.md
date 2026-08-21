# Elementum.Worker

Background ingestion and data consolidation daemon for **Elementum**. Periodically fetches precious metal spot prices, consolidates daily price candles, and executes data retention cleanup.

---

## Operational Features

1. **Scheduled Ingestion Job (`MetalsIngestionJob`):**
   - Fetches live quotes from the external Metals API.
   - Persists price history ticks to MySQL with Polly resilience retries.
2. **Distributed Locking (`IDistributedLockProvider`):**
   - Prevents concurrent ingestion runs when scaled across multiple instances.
3. **Daily Candle Consolidation & Retention:**
   - Consolidates tick history into daily OHLC summaries.
   - Cleans up raw tick data older than the configured retention period.
4. **Fail-Fast Configuration (`WorkerScheduleOptions`):**
   - Startup validation via `ValidateDataAnnotations().ValidateOnStart()`.
5. **Observability & Metrics (`IngestionMetrics`):**
   - Metrics are instrumented via `System.Diagnostics.Metrics` (`IngestionMetrics`), ready for OpenTelemetry export. No collector/exporter is wired up yet — this is a deliberate scope cut for the portfolio version.

---

## Configuration (`appsettings.Development.json`)

Copy `appsettings.Development.json.example` to `appsettings.Development.json` (gitignored, like `.env`) and replace the placeholders:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3307;Database=Elementum-Database;User=<MYSQL_USER>;Password=<MYSQL_PASSWORD>;"
  },
  "WorkerSchedule": {
    "IngestionIntervalMinutes": 60,
    "DailyRollupHour": 22,
    "RetentionDays": 7
  },
  "MetalsApi": {
    "BaseUrl": "https://api.edelmetalle.de/public.json",
    "Currency": "USD"
  }
}
```

---

## Running the Worker

### Daemon Mode (Continuous Background Service)
```bash
dotnet run --project src/Elementum.Worker
```

### Windows Service Installation (Optional)

1. **Publish:**
   ```powershell
   dotnet publish src/Elementum.Worker/Elementum.Worker.csproj -c Release -o bin/publish
   ```
2. **Register Windows Service (as Administrator):**
   ```cmd
   sc.exe create "Elementum.Worker" binpath= "C:\path\to\publish\Elementum.Worker.exe" start= auto
   sc.exe description "Elementum.Worker" "Fetches periodic metal prices into Elementum database."
   ```
3. **Start Service:**
   ```cmd
   sc.exe start "Elementum.Worker"
   ```
