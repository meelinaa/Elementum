using Elementum.Application.Models;
using Elementum.Application.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Elementum.Infrastructure.Caching;

/// <summary>
/// Infrastructure Decorator for <see cref="ILiveQuotesProvider"/> providing 5-minute In-Memory Caching.
/// Decouples caching concerns from the core Application domain services.
/// </summary>
public class CachedLiveQuotesProvider : ILiveQuotesProvider
{
    private const string CacheKey = "Edelmetalle_LiveQuotes";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private readonly ILiveQuotesProvider _inner;
    private readonly IMemoryCache _cache;

    public CachedLiveQuotesProvider(ILiveQuotesProvider inner, IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);

        _inner = inner;
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<EdelmetalleApiResponse> GetLiveQuoteAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out EdelmetalleApiResponse? cached) && cached != null)
        {
            return cached;
        }

        var quote = await _inner.GetLiveQuoteAsync(cancellationToken);
        _cache.Set(CacheKey, quote, DefaultCacheDuration);
        return quote;
    }
}
