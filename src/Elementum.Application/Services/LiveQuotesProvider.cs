using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Services;

/// <summary>
/// Default implementation of <see cref="ILiveQuotesProvider"/> with 5-minute in-memory caching and fallback resilience.
/// </summary>
public class LiveQuotesProvider : ILiveQuotesProvider
{
    private const string CacheKey = "Edelmetalle_LiveQuotes";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

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
                _cache.Set(CacheKey, quote, CacheDuration);
                return quote;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live quotes from api.edelmetalle.de; using fallback or defaults.");
        }

        // Fallback default quote if API unreachable
        return new EdelmetalleApiResponse
        {
            GoldUsd = 4410.60m,
            GoldEur = 3803.80m,
            SilberUsd = 65.71m,
            SilberEur = 56.68m,
            PlatinUsd = 1773.50m,
            PlatinEur = 1529.59m,
            PalladiumUsd = 1334.00m,
            PalladiumEur = 1150.53m,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WechselkursUsdEur = 1.1595m
        };
    }
}
