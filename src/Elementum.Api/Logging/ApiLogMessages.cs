using Microsoft.Extensions.Logging;

namespace Elementum.Api.Logging;

/// <summary>
/// Source-generated logging for the API host and pipelines.
/// </summary>
public static partial class ApiLogMessages
{
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "Elementum REST API service started.")]
    public static partial void ApiServiceStarted(ILogger logger);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "Trading analysis query returned no result for symbol '{Symbol}'.")]
    public static partial void TradingAnalysisNotFound(ILogger logger, string symbol);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Error,
        Message = "Unhandled exception during HTTP request {Method} {Path}.")]
    public static partial void UnhandledExceptionOccurred(ILogger logger, string method, string path, Exception ex);
}
