using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;

namespace Elementum.Infrastructure.Data.Interfaces;

/// <summary>
/// Abstraction for Elementum data access. Implemented by <see cref="ElementumDbContext"/>.
/// Allows tests to mock the context and keeps the API layer decoupled from EF Core.
/// </summary>
public interface IElementumDbContext
{
    /// <summary>Queryable over all rows in <c>metals</c> (deferred execution).</summary>
    IQueryable<Metals> QueryMetals();

    /// <summary>Queryable over <c>price_history</c> with <see cref="PriceHistory.Metal"/> included.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryAll();

    /// <summary>Queryable over price history for the given metal symbol (metal navigation included).</summary>
    IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol);

    /// <summary>Queryable over price history in the inclusive date range.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate);

    /// <summary>Queryable over price history for a metal symbol in the inclusive date range.</summary>
    IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate);

    /// <summary>Returns a single metal by its primary key.</summary>
    Task<Metals?> GetMetalById(int id, CancellationToken ct);

    /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG, XPT).</summary>
    Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct);

    /// <summary>Returns whether any row in <c>price_history</c> has entry date equal to today (UTC). Used by ingestion to avoid duplicate runs.</summary>
    Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct);

    /// <summary>Returns the most recent price history entry for a metal by symbol.</summary>
    Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct);

    /// <summary>Returns the latest price history entry per metal (grouped by symbol).</summary>
    Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken ct);

    /// <summary>Returns aggregated price history for a metal (daily/weekly/monthly/yearly) with at most <paramref name="count"/> entries.</summary>
    Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);
}
