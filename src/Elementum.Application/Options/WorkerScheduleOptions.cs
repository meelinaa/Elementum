using System.ComponentModel.DataAnnotations;

namespace Elementum.Application.Options;

/// <summary>
/// Configuration for the background worker daemon schedule, rollup, and retention.
/// Fail-fast validation with DataAnnotations without hardcoded fallback values.
/// </summary>
public class WorkerScheduleOptions
{
    public const string SectionName = "WorkerSchedule";

    /// <summary>Interval in minutes between price ingestion runs (1 to 1440 minutes).</summary>
    [Required(ErrorMessage = "WorkerSchedule:IngestionIntervalMinutes is required.")]
    [Range(1, 1440, ErrorMessage = "WorkerSchedule:IngestionIntervalMinutes must be between 1 and 1440 minutes.")]
    public int IngestionIntervalMinutes { get; set; }

    /// <summary>Hour of day (0-23 UTC) when daily candle summary is finalized.</summary>
    [Required(ErrorMessage = "WorkerSchedule:DailyRollupHour is required.")]
    [Range(0, 23, ErrorMessage = "WorkerSchedule:DailyRollupHour must be between 0 and 23.")]
    public int DailyRollupHour { get; set; }

    /// <summary>Retention period in days for raw hourly ticks before pruning (1 to 365 days).</summary>
    [Required(ErrorMessage = "WorkerSchedule:RetentionDays is required.")]
    [Range(1, 365, ErrorMessage = "WorkerSchedule:RetentionDays must be between 1 and 365 days.")]
    public int RetentionDays { get; set; }
}
