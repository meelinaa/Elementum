using Elementum.Domain.Entities;
using Elementum.Domain.Models;

namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Port: Abstraction for price history and metals persistence and querying.
/// </summary>
public interface IPriceHistoryRepository
{
    /// <summary>Saves incoming daily prices from external sources to the database.</summary>
    Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default);

    /// <summary>Returns whether any row in price_history has entry date equal to today (UTC).</summary>
    Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct);

    /// <summary>Queryable over all rows in metals.</summary>
    IQueryable<Metals> QueryMetals();

    /// <summary>Queryable over price_history with metal included.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryAll();

    /// <summary>Queryable over price history for the given metal symbol.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol);

    /// <summary>Queryable over price history in the inclusive date range.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate);

    /// <summary>Queryable over price history for a metal symbol in the inclusive date range.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate);

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

    /// <summary>Saves hourly quotes for all metals in USD & EUR from Edelmetalle API.</summary>
    Task SaveEdelmetallePricesAsync(EdelmetalleApiResponse data, CancellationToken ct = default);

    /// <summary>Aggregates the daily candle (Open, High, Low, Close at 22:00) into daily_price_summaries for the given date.</summary>
    Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default);

    /// <summary>Deletes hourly price_history records older than the specified UTC timestamp (7-day retention policy).</summary>
    Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default);

    /// <summary>Retrieves daily candle summaries (Min/Max/Open/Close) for a metal symbol and currency in a date range.</summary>
    Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(string symbol, string currency, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default);
}
