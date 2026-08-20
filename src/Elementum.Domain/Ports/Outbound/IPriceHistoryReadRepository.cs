using Elementum.Domain.Entities;

namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Read Port: Abstraction for querying price history, metals, and daily candle summaries.
/// Follows Interface Segregation Principle (ISP) / CQRS pattern for read-only consumption.
/// Returns materialized collections so Application never composes LINQ against the persistence model.
/// </summary>
public interface IPriceHistoryReadRepository
{
    /// <summary>Returns whether any row in price_history has entry date equal to today (UTC).</summary>
    Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct);

    /// <summary>Returns the full metals catalog.</summary>
    Task<IReadOnlyList<Metals>> GetMetalsAsync(CancellationToken ct = default);

    /// <summary>Returns price history ticks for a metal symbol, optionally filtered by currency (USD/EUR).</summary>
    Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAsync(
        string symbol,
        string? currency = null,
        CancellationToken ct = default);

    /// <summary>Returns price history ticks for a metal symbol in an inclusive date range, optionally filtered by currency.</summary>
    Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        CancellationToken ct = default);

    /// <summary>Returns a single metal by its primary key.</summary>
    Task<Metals?> GetMetalById(int id, CancellationToken ct);

    /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG, XPT).</summary>
    Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct);

    /// <summary>Returns the most recent price history entry for a metal by symbol.</summary>
    Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct);

    /// <summary>Returns the latest price history entry per metal (grouped by symbol).</summary>
    Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct);

    /// <summary>Returns aggregated price history for a metal (daily/weekly/monthly/yearly) with at most count entries.</summary>
    Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);

    /// <summary>Retrieves daily candle summaries (Min/Max/Open/Close) for a metal symbol and currency in a date range.</summary>
    Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(string symbol, string currency, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
}
