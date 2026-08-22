using Microsoft.Extensions.Logging;

namespace Elementum.Application.Logging;

/// <summary>
/// Source-generated, high-performance, zero-allocation logging for price ingestion use cases.
/// </summary>
public static partial class IngestionLogMessages
{
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Fetching precious metal prices from Edelmetalle API...")]
    public static partial void FetchingPrices(ILogger logger);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Error,
        Message = "Upstream Edelmetalle API data validation failed: {Errors}. Aborting persistence to protect database integrity.")]
    public static partial void UpstreamValidationFailed(ILogger logger, string errors);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Saving hourly precious metal quotes (Gold, Silber, Platin, Palladium in USD & EUR)...")]
    public static partial void SavingHourlyQuotes(ILogger logger);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Hourly price ingestion completed successfully.")]
    public static partial void HourlyIngestionSuccess(ILogger logger);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Information,
        Message = "Saving {Count} price records from fallback API...")]
    public static partial void SavingFallbackPrices(ILogger logger, int count);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Warning,
        Message = "No price data returned from external metals API.")]
    public static partial void NoPriceDataReturned(ILogger logger);

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Information,
        Message = "Primary upstream payload was empty, but today's metals catalog is already complete. Skipping fallback fetch.")]
    public static partial void SkippingFallbackCatalogComplete(ILogger logger);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Information,
        Message = "Aggregating daily {RollupHour}:00 candle summary for {Today}...")]
    public static partial void AggregatingDailyCandle(ILogger logger, int rollupHour, DateOnly today);

    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Information,
        Message = "Pruning hourly raw ticks older than {Threshold} ({RetentionDays}-day retention policy)...")]
    public static partial void PruningHourlyTicks(ILogger logger, DateTime threshold, int retentionDays);

    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Information,
        Message = "Retention cleanup completed: {Count} old hourly records purged.")]
    public static partial void RetentionCleanupCompleted(ILogger logger, int count);
}
