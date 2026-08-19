using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.External.Logging;

/// <summary>
/// Source-generated logging for external API health checks.
/// </summary>
public static partial class GoldApiHealthCheckLogMessages
{
    [LoggerMessage(
        EventId = 3301,
        Level = LogLevel.Warning,
        Message = "Edelmetalle API health check failed.")]
    public static partial void HealthCheckFailed(ILogger logger, Exception ex);
}
