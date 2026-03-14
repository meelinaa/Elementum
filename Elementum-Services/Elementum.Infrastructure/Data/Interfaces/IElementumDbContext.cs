using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data.Interfaces
{
    /// <summary>
    /// Abstraction for Elementum data access. Implemented by <see cref="ElementumDbContext"/>.
    /// Allows tests to mock the context and keeps the API layer decoupled from EF Core.
    /// </summary>
    public interface IElementumDbContext
    {
        /// <summary>Returns all metals from the <c>metals</c> table.</summary>
        Task<IEnumerable<Metals>> GetMetalsAll(CancellationToken ct);

        /// <summary>Returns a single metal by its primary key.</summary>
        Task<Metals?> GetMetalById(int id, CancellationToken ct);

        /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG, XPT).</summary>
        Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct);

        /// <summary>Returns whether any row in <c>price_history</c> has entry date equal to today (UTC). Used by ingestion to avoid duplicate runs.</summary>
        Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct);

        /// <summary>Returns the most recent price history entry for a metal by symbol.</summary>
        Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct);

        /// <summary>Returns all rows from the <c>price_history</c> table.</summary>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken ct);

        /// <summary>Returns the latest price history entry per metal (grouped by symbol).</summary>
        Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken ct);

        /// <summary>Returns price history for a specific metal by its symbol.</summary>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken ct);

        /// <summary>Returns all price history within a date range (inclusive).</summary>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken ct);

        /// <summary>Returns price history for a metal (by symbol) within a date range (inclusive).</summary>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct);

        /// <summary>Returns aggregated price history for a metal (daily/weekly/monthly/yearly) with at most <paramref name="count"/> entries.</summary>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);
    }
}
