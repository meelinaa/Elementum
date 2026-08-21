using Elementum.Domain.Entities;

namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Read Port: Abstraction for querying price history, metals, and daily candle summaries.
/// Follows Interface Segregation Principle (ISP) / CQRS pattern for read-only consumption.
/// Returns materialized collections so Application never composes LINQ against the persistence model.
/// </summary>
public interface IPriceHistoryReadRepository
{
    /// <summary>
    /// Returns whether every metal in the catalog has at least one <c>price_history</c> row for today (UTC).
    /// Used by ingestion to skip a redundant fallback fetch when the daily catalog is already complete.
    /// </summary>
    Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct = default);

    /// <summary>Returns the full metals catalog.</summary>
    Task<IReadOnlyList<Metals>> GetMetalsAsync(CancellationToken ct = default);

    /// <summary>Returns price history ticks for a metal symbol, optionally filtered by currency (USD/EUR).</summary>
    Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAsync(
        string symbol,
        string? currency = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a page of price history ticks for a metal in an inclusive date range, optionally filtered by currency.
    /// <paramref name="skip"/>/<paramref name="take"/> are applied after the filter; TotalCount is the unpaged match count.
    /// </summary>
    Task<(IReadOnlyList<PriceHistory> Items, int TotalCount)> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>Returns the most recent price history entry for a metal by symbol.</summary>
    Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct);

    /// <summary>Returns the latest price history ticks per metal, selected by MAX(ReferenceTimestamp) rather than MAX(Id).</summary>
    Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct);

    /// <summary>Returns aggregated price history for a metal (daily/weekly/monthly/yearly) with at most count entries.</summary>
    Task<IReadOnlyList<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);

    /// <summary>
    /// Returns daily candle summaries for all metals and currencies in the inclusive date range.
    /// Used by live-price orchestration so Application issues one filtered query instead of per-metal roundtrips.
    /// </summary>
    Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}
