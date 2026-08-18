using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Services;

/// <summary>
/// Default implementation of <see cref="ILiveQuotesProvider"/> with in-memory caching and fail-fast resilience.
/// </summary>
public class LiveQuotesProvider : ILiveQuotesProvider
{
    private const string CacheKey = "Edelmetalle_LiveQuotes";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMetalsApiClient _apiClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LiveQuotesProvider> _logger;

    public LiveQuotesProvider(
        IMetalsApiClient apiClient,
        IMemoryCache cache,
        ILogger<LiveQuotesProvider> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<EdelmetalleApiResponse> GetLiveQuoteAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out EdelmetalleApiResponse? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var quote = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);
            if (quote != null)
            {
                _cache.Set(CacheKey, quote, DefaultCacheDuration);
                return quote;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch live quotes from external metals API.");
            throw;
        }

        throw new InvalidOperationException("External metals API returned an empty live quote response.");
    }
}
