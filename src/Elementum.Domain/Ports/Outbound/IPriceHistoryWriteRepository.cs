using Elementum.Domain.Entities;

namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Write Port: Abstraction for price ingestion, candle aggregation, and retention pruning.
/// Follows Interface Segregation Principle (ISP) / CQRS pattern for write-only ingestion and background maintenance.
/// </summary>
public interface IPriceHistoryWriteRepository
{
    /// <summary>Saves price history records to the database and updates daily candles.</summary>
    Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default);

    /// <summary>Aggregates the daily candle (Open, High, Low, Close at 22:00) into daily_price_summaries for the given date.</summary>
    Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Deletes hourly price_history records older than the specified UTC timestamp (7-day retention policy).</summary>
    Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default);
}
