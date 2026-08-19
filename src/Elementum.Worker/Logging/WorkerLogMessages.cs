using Microsoft.Extensions.Logging;

namespace Elementum.Worker.Logging;

/// <summary>
/// Source-generated, high-performance, zero-allocation logging for the Worker background daemon.
/// </summary>
public static partial class WorkerLogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Elementum Ingestion Daemon initialized (Interval: {Interval}m, RollupHour: {Rollup}h, Retention: {Retention}d).")]
    public static partial void DaemonInitialized(ILogger logger, int interval, int rollup, int retention);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Executing initial precious metals ingestion on startup...")]
    public static partial void InitialIngestionStarting(ILogger logger);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Initial startup ingestion completed successfully.")]
    public static partial void InitialIngestionCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Worker daemon stopped during initial run.")]
    public static partial void WorkerDaemonStopped(ILogger logger);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Error during initial ingestion on startup. Daemon will continue with scheduled loop.")]
    public static partial void InitialIngestionFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "Scheduled ingestion cycle: next run in {DelayMinutes:F1}m ({NextUtc:yyyy-MM-dd HH:mm:ss} UTC).")]
    public static partial void ScheduledCycleWait(ILogger logger, double delayMinutes, DateTime nextUtc);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Information,
        Message = "Starting scheduled ingestion at {Time:yyyy-MM-dd HH:mm:ss} UTC...")]
    public static partial void ScheduledIngestionStarting(ILogger logger, DateTime time);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Information,
        Message = "Worker daemon loop cancelled due to host shutdown.")]
    public static partial void WorkerDaemonCancelled(ILogger logger);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Error,
        Message = "Unexpected error in worker execution loop. Retrying in {Minutes} minute...")]
    public static partial void WorkerLoopError(ILogger logger, double minutes, Exception ex);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Metals ingestion job cancelled as host is shutting down.")]
    public static partial void JobCancelledHostShutdown(ILogger logger);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Error,
        Message = "Unhandled exception during metals ingestion job execution. Host process remains running.")]
    public static partial void JobUnhandledError(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Information,
        Message = "Elementum Worker starting in Single-Run mode (--run-once / --once).")]
    public static partial void SingleRunStarting(ILogger logger);

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Information,
        Message = "Single-Run ingestion completed successfully.")]
    public static partial void SingleRunCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Information,
        Message = "Elementum Worker Service starting in Daemon mode.")]
    public static partial void DaemonStarting(ILogger logger);

    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Critical,
        Message = "Application terminated unexpectedly.")]
    public static partial void ApplicationTerminatedUnexpectedly(ILogger logger, Exception ex);
}
