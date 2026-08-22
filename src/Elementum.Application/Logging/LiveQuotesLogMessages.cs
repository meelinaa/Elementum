using Microsoft.Extensions.Logging;

namespace Elementum.Application.Logging;

/// <summary>
/// Source-generated logging for live quote services.
/// </summary>
public static partial class LiveQuotesLogMessages
{
    [LoggerMessage(
        EventId = 2101,
        Level = LogLevel.Error,
        Message = "Failed to fetch live quotes from external metals API.")]
    public static partial void FailedToFetchLiveQuotes(ILogger logger, Exception ex);
}
