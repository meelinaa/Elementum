using Microsoft.Extensions.Logging;

namespace Elementum.Worker.Logging;

/// <summary>
/// Source-generated, zero-allocation logging for metals ingestion jobs.
/// </summary>
public static partial class MetalsIngestionJobLogMessages
{
    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Warning,
        Message = "Ingestion job skipped: another worker instance currently holds distributed lock '{Resource}'.")]
    public static partial void JobSkippedLockHeld(ILogger logger, string resource);

    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Information,
        Message = "Metals ingestion job started (Distributed lock acquired).")]
    public static partial void JobStarted(ILogger logger);

    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Information,
        Message = "Metals ingestion job completed.")]
    public static partial void JobCompleted(ILogger logger);

    [LoggerMessage(
        EventId = 1104,
        Level = LogLevel.Error,
        Message = "Metals ingestion job failed.")]
    public static partial void JobFailed(ILogger logger, Exception ex);
}
