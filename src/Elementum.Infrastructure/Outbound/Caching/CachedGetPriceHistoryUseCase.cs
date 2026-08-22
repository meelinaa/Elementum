using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Microsoft.Extensions.Caching.Hybrid;

namespace Elementum.Infrastructure.Outbound.Caching;

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

    public async ValueTask<PriceHistoryPageDto> GetBySymbolAsync(
        string symbol,
        string? currency = null,
        string? from = null,
        string? to = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        var cur = (currency ?? string.Empty).Trim().ToUpperInvariant();
        var fromKey = (from ?? string.Empty).Trim();
        var toKey = (to ?? string.Empty).Trim();
        var effectiveSkip = skip < 0 ? 0 : skip;
        var effectiveTake = take ?? 0;
        var cacheKey = $"prices:symbol:{normalized}:{cur}:{fromKey}:{toKey}:{effectiveSkip}:{effectiveTake}";

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async token => await _inner.GetBySymbolAsync(normalized, currency, from, to, skip, take, token),
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

    public ValueTask<PriceHistoryPageDto> GetByDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default)
    {
        return _inner.GetByDateRangeAsync(symbol, firstDate, lastDate, currency, skip, take, ct);
    }
}
