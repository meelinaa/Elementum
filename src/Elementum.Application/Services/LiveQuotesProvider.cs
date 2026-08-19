using Elementum.Application.Exceptions;
using Elementum.Application.Logging;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Services;

/// <summary>
/// Default implementation of <see cref="ILiveQuotesProvider"/> with in-memory caching and fail-fast resilience.
/// Uses <see cref="LiveQuotesLogMessages"/> for zero-allocation logging and Exception Factories.
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
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        _apiClient = apiClient;
        _cache = cache;
        _logger = logger;
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
            LiveQuotesLogMessages.FailedToFetchLiveQuotes(_logger, ex);
            throw;
        }

        throw ExternalApiException.EmptyLiveQuoteResponse();
    }
}
