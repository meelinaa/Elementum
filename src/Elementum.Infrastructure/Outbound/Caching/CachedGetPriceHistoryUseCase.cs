using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Microsoft.Extensions.Caching.Hybrid;

namespace Elementum.Infrastructure.Caching;

/// <summary>
/// Decorator for <see cref="IGetPriceHistoryUseCase"/> providing 2-tier caching (L1 Memory + L2 Distributed Redis)
/// and automatic Stampede Protection (concurrency lock per key) using .NET HybridCache.
/// Returns <see cref="ValueTask{TResult}"/> to ensure zero heap allocations on L1 memory cache hits.
/// </summary>
public class CachedGetPriceHistoryUseCase : IGetPriceHistoryUseCase
{
    private readonly IGetPriceHistoryUseCase _inner;
    private readonly HybridCache _cache;
    private static readonly HybridCacheEntryOptions SpotPriceOptions = new()
    {
        Expiration = TimeSpan.FromSeconds(60),
        LocalCacheExpiration = TimeSpan.FromSeconds(60)
    };

    public CachedGetPriceHistoryUseCase(IGetPriceHistoryUseCase inner, HybridCache cache)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
    }

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default)
    {
        return await _cache.GetOrCreateAsync(
            "prices:all:latest",
            async token => (await _inner.GetLatestAllAsync(token)).ToList(),
            SpotPriceOptions,
            cancellationToken: ct);
    }

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, string? currency = null, CancellationToken ct = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        var cur = (currency ?? string.Empty).Trim().ToUpperInvariant();
        var cacheKey = string.IsNullOrEmpty(cur) ? $"prices:symbol:{normalized}" : $"prices:symbol:{normalized}:{cur}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async token => (await _inner.GetBySymbolAsync(normalized, currency, token)).ToList(),
            SpotPriceOptions,
            cancellationToken: ct);
    }

    public async ValueTask<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        return await _cache.GetOrCreateAsync(
            $"price:latest:{normalized}",
            async token => await _inner.GetLatestBySymbolAsync(normalized, token),
            SpotPriceOptions,
            cancellationToken: ct);
    }

    public async ValueTask<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        return await _cache.GetOrCreateAsync(
            $"trading:latest:{normalized}",
            async token => await _inner.GetTradingLatestAsync(normalized, token),
            SpotPriceOptions,
            cancellationToken: ct);
    }

    public ValueTask<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default)
    {
        return _inner.GetByDateRangeAsync(symbol, firstDate, lastDate, ct);
    }

    public ValueTask<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default)
    {
        return _inner.GetAggregatedAsync(symbol, aggregation, count, ct);
    }
}
