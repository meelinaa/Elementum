using Elementum.Shared.Objects;

namespace Elementum_ServiceApi.Services.Interfaces
{
    /// <summary>
    /// Application service for the Elementum API. Exposes read operations for metals and price history.
    /// Controllers depend on this interface for testability and clear layering.
    /// </summary>
    public interface IApiService
    {
        /// <summary>Returns all metals from the database.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>List of all metals.</returns>
        Task<IEnumerable<Metals>> GetAllMetals(CancellationToken cancellationToken);

        /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG).</summary>
        /// <param name="symbol">The metal symbol.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The metal if found; otherwise <c>null</c>.</returns>
        Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns all price history entries from the database.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>List of all price history records.</returns>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken cancellationToken);

        /// <summary>Returns the latest price history entry per metal (for dashboard/current prices).</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>One latest entry per metal.</returns>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken cancellationToken);

        /// <summary>Returns price history for a specific metal by its symbol.</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the given metal.</returns>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns the most recent price history entry for a metal by symbol.</summary>
        /// <param name="symbol">The metal symbol.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The latest price history entry for the metal.</returns>
        Task<PriceHistory> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns all price history within a date range (inclusive).</summary>
        /// <param name="firstDate">Start date of the range.</param>
        /// <param name="lastDate">End date of the range.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history entries within the date range.</returns>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken);

        /// <summary>Returns price history for a metal (by symbol) within a date range (inclusive).</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="firstDate">Start date of the range.</param>
        /// <param name="lastDate">End date of the range.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the metal within the date range.</returns>
        Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken);
        Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);
    }
}
