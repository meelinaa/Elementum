using System.Net;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Logging;

/// <summary>
/// Source-generated logging for the CLI application.
/// </summary>
public static partial class CliLogMessages
{
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Failed to load ApiBaseUrl from configuration files.")]
    public static partial void ConfigLoadFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Warning,
        Message = "Failed to load live overview.")]
    public static partial void LiveOverviewLoadFailed(ILogger logger, Exception ex);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Warning,
        Message = "Failed to load metal details for {Symbol}.")]
    public static partial void MetalDetailsLoadFailed(ILogger logger, string symbol, Exception ex);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Warning,
        Message = "Failed to load price history for {Symbol} ({Currency}).")]
    public static partial void PriceHistoryLoadFailed(ILogger logger, string symbol, string currency, Exception ex);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Warning,
        Message = "Request failed: {Endpoint} -> {StatusCode}")]
    public static partial void HttpRequestFailed(ILogger logger, string endpoint, HttpStatusCode statusCode);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Warning,
        Message = "Request failed: {Endpoint}")]
    public static partial void RequestException(ILogger logger, string endpoint, Exception ex);

    [LoggerMessage(
        EventId = 4007,
        Level = LogLevel.Warning,
        Message = "Load operation failed.")]
    public static partial void LoadOperationFailed(ILogger logger, Exception ex);
}
