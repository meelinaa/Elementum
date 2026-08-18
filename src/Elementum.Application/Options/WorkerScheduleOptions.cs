namespace Elementum.Application.Options;

/// <summary>
/// Configuration for the background worker daemon schedule, rollup, and retention.
/// </summary>
public class WorkerScheduleOptions
{
    public const string SectionName = "WorkerSchedule";

    /// <summary>Interval between hourly price ingestion runs (default: 60 minutes).</summary>
    public int IngestionIntervalMinutes { get; set; } = 60;

    /// <summary>Hour of day (0-23) when daily candle summary is finalized (default: 22:00 UTC/local).</summary>
    public int DailyRollupHour { get; set; } = 22;

    /// <summary>Retention period in days for raw hourly ticks before pruning (default: 7 days).</summary>
    public int RetentionDays { get; set; } = 7;
}
