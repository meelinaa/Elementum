using System.Diagnostics.Metrics;

namespace Elementum.WorkerService.Observability;

/// <summary>
/// Metrics for the metals ingestion job (runs, errors) for production observability.
/// </summary>
public sealed class IngestionMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _runsTotal;
    private readonly Counter<long> _errorsTotal;
    private readonly Counter<long> _pricesSavedTotal;

    /// <summary>Creates the meter and counters for ingestion runs, errors, and prices saved.</summary>
    public IngestionMetrics()
    {
        _meter = new Meter("Elementum.WorkerService", "1.0");
        _runsTotal = _meter.CreateCounter<long>("ingestion_runs_total", description: "Total number of ingestion job runs");
        _errorsTotal = _meter.CreateCounter<long>("ingestion_errors_total", description: "Total number of ingestion errors");
        _pricesSavedTotal = _meter.CreateCounter<long>("ingestion_prices_saved_total", description: "Total number of price rows saved");
    }

    /// <summary>Increments the ingestion runs counter by one.</summary>
    public void RecordRun() => _runsTotal.Add(1);

    /// <summary>Increments the ingestion errors counter by one, with optional reason tag.</summary>
    public void RecordError(string? reason = null) => _errorsTotal.Add(1, new KeyValuePair<string, object?>("reason", reason ?? "unknown"));

    /// <summary>Adds the given count to the prices-saved counter.</summary>
    public void RecordPricesSaved(int count) => _pricesSavedTotal.Add(count);
}
