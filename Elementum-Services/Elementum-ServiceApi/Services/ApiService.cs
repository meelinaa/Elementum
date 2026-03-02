using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.Objects;
using Elementum_ServiceApi.Services.Interfaces;

namespace Elementum_ServiceApi.Services
{
    /// <summary>
    /// Service layer for the Elementum API. Delegates data access to <see cref="IElementumDbContext"/>.
    /// </summary>
    public class ApiService : IApiService
    {
        private readonly IElementumDbContext _db;

        public ApiService(IElementumDbContext db)
        {
            _db = db;
        }

        #region Metals

        public async Task<IEnumerable<Metals>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _db.GetMetalsAll(cancellationToken);
        }

        public async Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken cancellationToken)
        {
            return await _db.GetMetalBySymbol(symbol, cancellationToken);
        }

        #endregion Metals

        #region PriceHistory

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAll(cancellationToken);
        }

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAllLatest(cancellationToken);
        }

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalSymbol(symbol, cancellationToken);
        }

        public async Task<PriceHistory> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken cancellationToken)
        {
            var result = await _db.GetPriceHistoryByMetalSymbolLatest(symbol, cancellationToken);
            if (result == null)
                throw new InvalidOperationException($"No price history found for symbol '{symbol}'.");
            return result;
        }

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAllByDateRange(firstDate, lastDate, cancellationToken);
        }

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate, cancellationToken);
        }

        #endregion PriceHistory
    }
}
