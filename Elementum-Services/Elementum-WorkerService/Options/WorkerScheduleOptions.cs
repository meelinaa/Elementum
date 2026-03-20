namespace Elementum_WorkerService.Options;

/// <summary>
/// Scheduling options for the background <see cref="Worker"/> (daily metals ingestion).
/// Bound from the <c>Worker</c> configuration section (see <see cref="SectionName"/>).
/// </summary>
/// <remarks>
/// Override at runtime with environment variables, e.g. <c>Worker__DailyRunTime=02:30:00</c> (double underscore nests the section).
/// </remarks>
public record WorkerScheduleOptions
{
    /// <summary>Configuration section key in appsettings.json.</summary>
    public const string SectionName = "Worker";

    /// <summary>
    /// Local time of day when the scheduled daily ingestion runs (uses <see cref="DateTime.Now"/> / machine local timezone).
    /// JSON format: <c>"HH:mm:ss"</c> (e.g. <c>"23:00:00"</c>).
    /// </summary>
    public TimeSpan DailyRunTime { get; init; } = new(23, 0, 0);
}
