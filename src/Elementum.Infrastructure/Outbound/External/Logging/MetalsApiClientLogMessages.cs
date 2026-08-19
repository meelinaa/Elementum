using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.External.Logging;

/// <summary>
/// Source-generated logging for external metals API interactions.
/// </summary>
public static partial class MetalsApiClientLogMessages
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Streaming precious metal prices from {Url}...")]
    public static partial void StreamingPricesFromUrl(ILogger logger, string url);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Empty payload received from {Url}")]
    public static partial void EmptyPayloadReceived(ILogger logger, string url);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "Successfully fetched prices: Gold USD={GoldUsd}, EUR={GoldEur}, Silber USD={SilberUsd}, EUR={SilberEur}, Rate={Rate}")]
    public static partial void PricesFetchedSuccessfully(
        ILogger logger,
        decimal goldUsd,
        decimal goldEur,
        decimal silberUsd,
        decimal silberEur,
        decimal rate);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Error,
        Message = "Failed to fetch precious metal prices from {Url}")]
    public static partial void FailedToFetchPrices(ILogger logger, string url, Exception ex);
}
