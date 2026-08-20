using Elementum.Infrastructure.Outbound.Data;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Outbound.Data.Services;

/// <summary>
/// Service implementation for pruning stale hourly price history ticks based on a retention threshold.
/// </summary>
public class PriceHistoryPruner : IPriceHistoryPruner
{
    /// <inheritdoc />
    public async Task<int> PruneHourlyDataOlderThanAsync(ElementumDbContext db, DateTime thresholdUtc, CancellationToken ct = default)
    {
        var thresholdDate = DateOnly.FromDateTime(thresholdUtc);

        var staleRecords = await db.PriceHistory
            .Where(p => p.EntryDate < thresholdDate)
            .ToListAsync(ct);

        if (staleRecords.Count == 0)
            return 0;

        db.PriceHistory.RemoveRange(staleRecords);
        await db.SaveChangesAsync(ct);
        return staleRecords.Count;
    }
}
