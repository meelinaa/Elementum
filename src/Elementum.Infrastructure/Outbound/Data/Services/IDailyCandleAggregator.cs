using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data;

namespace Elementum.Infrastructure.Outbound.Data.Services;

/// <summary>
/// Service responsible for aggregating and updating daily price candles (Min, Max, Open, Close).
/// </summary>
public interface IDailyCandleAggregator
{
    /// <summary>
    /// Aggregates hourly price ticks for a given date and persists daily candle summaries (Open, High, Low, Close).
    /// </summary>
    Task AggregateDailySummaryAsync(ElementumDbContext db, DateOnly date, CancellationToken ct = default);

    /// <summary>
    /// Stages a price tick onto the daily summary candle within the EF Core change tracker (pending mutation).
    /// Does not call <c>SaveChangesAsync</c>; the caller is responsible for committing the changes in a unit-of-work transaction.
    /// </summary>
    Task StageSummaryTickAsync(
        ElementumDbContext db,
        int metalId,
        string currency,
        DateOnly date,
        decimal price,
        decimal exchangeRateUsdEur,
        bool isCloseHour,
        CancellationToken ct = default);
}
