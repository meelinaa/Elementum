namespace Elementum.Application.Options;

/// <summary>
/// Configuration for the background worker schedule.
/// </summary>
public class WorkerScheduleOptions
{
    public const string SectionName = "WorkerSchedule";

    public TimeSpan DailyRunTime { get; set; } = new TimeSpan(6, 0, 0);
    public int IntervalHours { get; set; } = 24;
}
