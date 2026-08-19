using Microsoft.Extensions.Logging;

namespace Elementum.Application.Logging;

/// <summary>
/// Source-generated logging for daily candle price queries.
/// </summary>
public static partial class CandlesLogMessages
{
    [LoggerMessage(
        EventId = 2201,
        Level = LogLevel.Debug,
        Message = "Retrieved {Count} daily candles for metal '{Symbol}' in '{Currency}'.")]
    public static partial void DailyCandlesQueried(ILogger logger, string symbol, string currency, int count);

    [LoggerMessage(
        EventId = 2202,
        Level = LogLevel.Warning,
        Message = "Metal not found for daily candles query: '{Symbol}'.")]
    public static partial void DailyCandlesMetalNotFound(ILogger logger, string symbol);
}
