using Elementum.Infrastructure.Data;

namespace Elementum.Infrastructure.Data.Services;

/// <summary>
/// Service responsible for purging stale hourly price ticks based on data retention policies.
/// </summary>
public interface IPriceHistoryPruner
{
    /// <summary>
    /// Deletes hourly price_history records older than the specified UTC timestamp.
    /// </summary>
    Task<int> PruneHourlyDataOlderThanAsync(ElementumDbContext db, DateTime thresholdUtc, CancellationToken ct = default);
}
