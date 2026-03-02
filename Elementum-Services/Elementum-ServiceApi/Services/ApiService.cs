using Elementum.Infrastructure.Data;
using Elementum.Shared.Objects;

namespace Elementum_ServiceApi.Services
{
    /// <summary>
    /// Service layer for the Elementum API. Delegates data access to <see cref="ElementumDbContext"/>.
    /// </summary>
    public class ApiService
    {
        private readonly ElementumDbContext _db;

        public ApiService(ElementumDbContext db)
        {
            _db = db;
        }

        #region Metals

        /// <summary>Returns all metals from the database.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>List of all metals.</returns>
        public async Task<IEnumerable<Metals>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _db.GetMetalsAll(cancellationToken);
        }

        /// <summary>Returns a single metal by its primary key.</summary>
        /// <param name="id">The metal ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The metal if found; otherwise <c>null</c>.</returns>
        public async Task<Metals?> GetMetalById(int id, CancellationToken cancellationToken)
        {
            return await _db.GetMetalById(id, cancellationToken);
        }

        /// <summary>Returns a single metal by its symbol (e.g. XAU, XAG).</summary>
        /// <param name="symbol">The metal symbol.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>The metal if found; otherwise <c>null</c>.</returns>
        public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken cancellationToken)
        {
            return await _db.GetMetalBySymbol(symbol, cancellationToken);
        }

        #endregion Metals

        #region PriceHistory

        /// <summary>Returns all price history entries from the database.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>List of all price history records.</returns>
        public async Task<IEnumerable<PriceHistory>> GetAllPriceHistory(CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAll(cancellationToken);
        }

        /// <summary>Returns price history for a specific metal by its ID.</summary>
        /// <param name="metalId">The metal ID.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the given metal.</returns>
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalId(int metalId, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalId(metalId, cancellationToken);
        }

        /// <summary>Returns price history for a specific metal by its symbol.</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the given metal.</returns>
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalSymbol(symbol, cancellationToken);
        }

        /// <summary>Returns all price history within a date range (inclusive).</summary>
        /// <param name="firstDate">Start date of the range.</param>
        /// <param name="lastDate">End date of the range.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history entries within the date range.</returns>
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAllByDateRange(firstDate, lastDate, cancellationToken);
        }

        /// <summary>Returns price history for a metal (by ID) within a date range (inclusive).</summary>
        /// <param name="metalId">The metal ID.</param>
        /// <param name="firstDate">Start date of the range.</param>
        /// <param name="lastDate">End date of the range.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the metal within the date range.</returns>
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalIdAndDateRange(int metalId, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalIdAndDateRange(metalId, firstDate, lastDate, cancellationToken);
        }

        /// <summary>Returns price history for a metal (by symbol) within a date range (inclusive).</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="firstDate">Start date of the range.</param>
        /// <param name="lastDate">End date of the range.</param>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>Price history for the metal within the date range.</returns>
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate, cancellationToken);
        }

        #endregion PriceHistory
    }
}
