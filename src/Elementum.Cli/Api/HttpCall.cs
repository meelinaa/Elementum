using System.Collections.Concurrent;
using System.Text.Json;
using Elementum.Application.DTOs;
using Elementum.Cli.Exceptions;
using Elementum.Cli.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Api;

/// <summary>
/// HTTP client and cache layer for the CLI. Timeout is owned by the injected <see cref="HttpClient"/>.
/// </summary>
public sealed class HttpCall : IHttpCall
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<HttpCall> _log;
    private readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    public HttpCall(HttpClient httpClient, IMemoryCache cache, ILogger<HttpCall> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _cache = cache;
        _log = logger;
    }

    /// <inheritdoc />
    public async Task<LiveMarketOverviewDto?> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendRequestAsync<LiveMarketOverviewDto>("prices/live", cancellationToken);
        }
        catch (Exception ex)
        {
            CliLogMessages.LiveOverviewLoadFailed(_log, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TradingPriceDto?> GetPriceHistoryTradingLatestAsync(
        string metalSymbol,
        string currency = "EUR",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendRequestAsync<TradingPriceDto>(
                $"prices/live/trading/{metalSymbol}?currency={currency}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            CliLogMessages.MetalDetailsLoadFailed(_log, metalSymbol, ex);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalAsync(
        string metalSymbol,
        string currency = "EUR",
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await SendRequestAsync<List<PriceHistoryDto>>(
                $"history/{metalSymbol}?currency={currency}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            CliLogMessages.PriceHistoryLoadFailed(_log, metalSymbol, currency, ex);
            return null;
        }
    }

    private async Task<T> SendRequestAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            if (_cache.TryGetValue(endpoint, out T? cachedValue) && cachedValue != null)
                return cachedValue;

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                CliLogMessages.HttpRequestFailed(_log, endpoint, response.StatusCode);
                throw CliHttpException.RequestFailed(response.StatusCode, endpoint);
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var value = JsonSerializer.Deserialize<T>(json, DefaultJsonOptions)!;

            _cacheKeys.TryAdd(endpoint, 0);
            _cache.Set(endpoint, value, DefaultCacheDuration);

            return value;
        }
        catch (Exception ex)
        {
            CliLogMessages.RequestException(_log, endpoint, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        foreach (var key in _cacheKeys.Keys)
            _cache.Remove(key);
        _cacheKeys.Clear();
    }
}
