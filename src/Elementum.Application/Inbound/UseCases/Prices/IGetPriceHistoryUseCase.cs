using Elementum.Application.DTOs;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Primary / Inbound Port: Queries price history, latest prices, trading data, karat calculations, and aggregations.
/// </summary>
public interface IGetPriceHistoryUseCase
{
    Task<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default);
    Task<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default);
    Task<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default);
    Task<KaratPricesDto?> GetKaratLatestAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default);
    Task<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default);
}
