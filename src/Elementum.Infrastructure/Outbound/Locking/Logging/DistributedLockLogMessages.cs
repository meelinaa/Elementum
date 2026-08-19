using Microsoft.Extensions.Logging;

namespace Elementum.Infrastructure.Locking.Logging;

/// <summary>
/// Source-generated, zero-allocation logging for distributed locking operations.
/// </summary>
public static partial class DistributedLockLogMessages
{
    [LoggerMessage(
        EventId = 3101,
        Level = LogLevel.Information,
        Message = "Acquired new EF Core distributed lock for '{Resource}' by '{AcquiredBy}' until {ExpiresAtUtc} UTC.")]
    public static partial void LockAcquired(ILogger logger, string resource, string acquiredBy, DateTime expiresAtUtc);

    [LoggerMessage(
        EventId = 3102,
        Level = LogLevel.Debug,
        Message = "Distributed lock '{Resource}' currently held by '{AcquiredBy}' until {ExpiresAtUtc} UTC.")]
    public static partial void LockBusy(ILogger logger, string resource, string acquiredBy, DateTime expiresAtUtc);

    [LoggerMessage(
        EventId = 3103,
        Level = LogLevel.Information,
        Message = "Took over expired EF Core distributed lock for '{Resource}' by '{AcquiredBy}' until {ExpiresAtUtc} UTC.")]
    public static partial void StaleLockTakenOver(ILogger logger, string resource, string acquiredBy, DateTime expiresAtUtc);

    [LoggerMessage(
        EventId = 3104,
        Level = LogLevel.Information,
        Message = "Renewed own active distributed lock for '{Resource}' by '{AcquiredBy}' until {ExpiresAtUtc} UTC.")]
    public static partial void LockRenewedManually(ILogger logger, string resource, string acquiredBy, DateTime expiresAtUtc);

    [LoggerMessage(
        EventId = 3105,
        Level = LogLevel.Warning,
        Message = "Failed to acquire EF Core distributed lock for '{Resource}'.")]
    public static partial void LockAcquisitionFailed(ILogger logger, string resource, Exception? ex);

    [LoggerMessage(
        EventId = 3106,
        Level = LogLevel.Debug,
        Message = "Auto-renewed distributed lock for '{Resource}' until {ExpiresAtUtc} UTC.")]
    public static partial void LockAutoRenewed(ILogger logger, string resource, DateTime expiresAtUtc);

    [LoggerMessage(
        EventId = 3107,
        Level = LogLevel.Warning,
        Message = "Lost ownership of distributed lock for '{Resource}' during heartbeat.")]
    public static partial void LockOwnershipLost(ILogger logger, string resource);

    [LoggerMessage(
        EventId = 3108,
        Level = LogLevel.Warning,
        Message = "Concurrency collision renewing distributed lock for '{Resource}'.")]
    public static partial void LockRenewalCollision(ILogger logger, string resource);

    [LoggerMessage(
        EventId = 3109,
        Level = LogLevel.Warning,
        Message = "Failed to renew distributed lock for '{Resource}'.")]
    public static partial void LockRenewalFailed(ILogger logger, string resource, Exception ex);

    [LoggerMessage(
        EventId = 3110,
        Level = LogLevel.Debug,
        Message = "Released EF Core distributed lock for '{Resource}'")]
    public static partial void LockReleased(ILogger logger, string resource);

    [LoggerMessage(
        EventId = 3111,
        Level = LogLevel.Warning,
        Message = "Error releasing EF Core distributed lock for '{Resource}'.")]
    public static partial void LockReleaseError(ILogger logger, string resource, Exception ex);
}
