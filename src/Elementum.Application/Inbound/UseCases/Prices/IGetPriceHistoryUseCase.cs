using Elementum.Application.DTOs;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Primary / Inbound Port: Queries price history, latest prices, trading data, and aggregations.
/// Uses <see cref="ValueTask{TResult}"/> to eliminate heap task allocations on synchronous cache hits.
/// </summary>
public interface IGetPriceHistoryUseCase
{
    ValueTask<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default);
    ValueTask<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, string? currency = null, CancellationToken ct = default);
    ValueTask<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default);
    ValueTask<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default);
    ValueTask<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default);
    ValueTask<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default);
}
