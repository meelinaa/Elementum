using Elementum.Application.DTOs;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Primary / Inbound Port: Queries price history, latest prices, and trading data.
/// Uses <see cref="ValueTask{TResult}"/> to eliminate heap task allocations on synchronous cache hits.
/// </summary>
public interface IGetPriceHistoryUseCase
{
    ValueTask<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Paged history for one metal. Omitted <paramref name="from"/>/<paramref name="to"/> default to the last 30 days;
    /// omitted <paramref name="take"/> defaults to 500 and is capped at 2000.
    /// </summary>
    ValueTask<PriceHistoryPageDto> GetBySymbolAsync(
        string symbol,
        string? currency = null,
        string? from = null,
        string? to = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default);

    ValueTask<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default);
    ValueTask<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default);
    ValueTask<PriceHistoryPageDto> GetByDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default);
}
