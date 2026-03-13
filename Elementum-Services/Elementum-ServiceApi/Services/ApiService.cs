using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.DTOs;
using Elementum.Shared.Mapping;
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

        public async Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken)
        {
            var list = await _db.GetMetalsAll(cancellationToken);
            return list.Select(m => m.ToDto());
        }

        public async Task<MetalsDto?> GetMetalBySymbol(string symbol, CancellationToken cancellationToken)
        {
            var metal = await _db.GetMetalBySymbol(symbol, cancellationToken);
            return metal?.ToDto();
        }

        #endregion Metals

        #region PriceHistory

        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAll(cancellationToken);
        }

        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
        {
            return await _db.GetPriceHistoryAllLatest(cancellationToken);
        }

        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken)
        {
            var list = await _db.GetPriceHistoryByMetalSymbol(symbol, cancellationToken);
            return list.Select(ph => ph.ToPriceHistoryDto());
        }

        public async Task<PriceHistory> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken cancellationToken)
        {
            var result = await _db.GetPriceHistoryByMetalSymbolLatest(symbol, cancellationToken);
            if (result == null)
                throw new InvalidOperationException($"No price history found for symbol '{symbol}'.");
            return result;
        }

        public async Task<TradingPriceDto?> GetPriceHistoryTradingLatest(string symbol, CancellationToken cancellationToken)
        {
            var entity = await _db.GetPriceHistoryByMetalSymbolLatest(symbol, cancellationToken);
            return entity?.ToTradingPriceDto();
        }

        public async Task<KaratPricesDto?> GetPriceHistoryKaratLatest(string symbol, CancellationToken cancellationToken)
        {
            var entity = await _db.GetPriceHistoryByMetalSymbolLatest(symbol, cancellationToken);
            return entity?.ToKaratPricesDto();
        }

        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            var list = await _db.GetPriceHistoryAllByDateRange(firstDate, lastDate, cancellationToken);
            return list.Select(ph => ph.ToPriceHistoryDto());
        }

        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken)
        {
            var list = await _db.GetPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate, cancellationToken);
            return list.Select(ph => ph.ToPriceHistoryDto());
        }

        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct)
        {
            var list = await _db.GetPriceHistoryMetalData(metalSymbol, aggregation, count, ct);
            return list.Select(ph => ph.ToPriceHistoryDto());
        }

        #endregion PriceHistory
    }
}
