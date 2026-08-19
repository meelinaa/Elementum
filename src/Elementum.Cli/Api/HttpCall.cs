using System.Collections.Concurrent;
using System.Text.Json;
using Elementum.Application.DTOs;
using Elementum.Cli.Config;
using Elementum.Cli.Exceptions;
using Elementum.Cli.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Api;

/// <summary>
/// Central HTTP client and cache layer for the CLI.
/// Handles REST API requests to Elementum API with in-memory caching and error resilience without magic literals.
/// Uses <see cref="CliLogMessages"/> for zero-allocation logging and Exception Factories.
/// </summary>
public class HttpCall
{
    private static readonly TimeSpan DefaultHttpTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

    private static readonly ILogger _log = CliLogging.GetLogger(nameof(HttpCall));

    /// <summary>Shared options for JSON (de)serialization.</summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Lazy<HttpClient> _httpClient = new(() =>
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(CliConfig.ApiBaseUrl),
            Timeout = DefaultHttpTimeout
        };
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    });

    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    /// <summary>GET prices/live — live market overview for all 4 metals in USD and EUR.</summary>
    public static async Task<LiveMarketOverviewDto?> GetLiveMarketOverviewAsync()
    {
        try
        {
            return await SendRequestAsync<LiveMarketOverviewDto>("prices/live");
        }
        catch (Exception ex)
        {
            CliLogMessages.LiveOverviewLoadFailed(_log, ex);
            return null;
        }
    }

    /// <summary>GET prices/live/trading/{symbol}?currency={currency} — live trading analysis for a specific metal.</summary>
    public static async Task<TradingPriceDto?> GetPriceHistoryTradingLatestAsync(string metalSymbol, string currency = "EUR")
    {
        try
        {
            return await SendRequestAsync<TradingPriceDto>($"prices/live/trading/{metalSymbol}?currency={currency}");
        }
        catch (Exception ex)
        {
            CliLogMessages.MetalDetailsLoadFailed(_log, metalSymbol, ex);
            return null;
        }
    }

    /// <summary>GET history/{symbol}?currency={currency} — returns raw price history for one metal and currency.</summary>
    public static async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalAsync(string metalSymbol, string currency = "EUR")
    {
        try
        {
            return await SendRequestAsync<List<PriceHistoryDto>>($"history/{metalSymbol}?currency={currency}");
        }
        catch (Exception ex)
        {
            CliLogMessages.PriceHistoryLoadFailed(_log, metalSymbol, currency, ex);
            return null;
        }
    }

    /// <summary>Sends GET to the given endpoint; returns cached JSON if present, otherwise fetches and caches.</summary>
    private static async Task<T> SendRequestAsync<T>(string endpoint)
    {
        try
        {
            if (_cache.TryGetValue(endpoint, out T? cachedValue) && cachedValue != null)
            {
                return cachedValue;
            }

            var response = await _httpClient.Value.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                CliLogMessages.HttpRequestFailed(_log, endpoint, response.StatusCode);
                throw CliHttpException.RequestFailed(response.StatusCode, endpoint);
            }

            var json = await response.Content.ReadAsStringAsync();
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

    /// <summary>Clears all cached API responses.</summary>
    public static void ClearCache()
    {
        foreach (var key in _cacheKeys.Keys)
            _cache.Remove(key);
        _cacheKeys.Clear();
    }
}