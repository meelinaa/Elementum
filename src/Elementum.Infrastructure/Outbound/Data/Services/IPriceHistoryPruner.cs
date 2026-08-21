using Elementum.Infrastructure.Outbound.Data;

namespace Elementum.Infrastructure.Outbound.Data.Services;

/// <summary>
/// Service responsible for purging stale hourly price ticks based on data retention policies.
/// </summary>
public interface IPriceHistoryPruner
{
    /// <summary>
    /// Deletes hourly <c>price_history</c> rows older than the cutoff via <c>ExecuteDeleteAsync</c> (no tracked load).
    /// </summary>
    Task<int> PruneHourlyDataOlderThanAsync(ElementumDbContext db, DateTime thresholdUtc, CancellationToken ct = default);
}
