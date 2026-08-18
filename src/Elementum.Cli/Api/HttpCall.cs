using System.Collections.Concurrent;
using System.Text.Json;
using Elementum.Application.DTOs;
using Elementum.Cli.Config;
using Elementum.Cli.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Api;

/// <summary>
/// Central HTTP client and cache layer for the CLI.
/// Handles REST API requests to Elementum API with in-memory caching and error resilience.
/// </summary>
public class HttpCall
{
    private static readonly ILogger _log = CliLogging.GetLogger(nameof(HttpCall));

    /// <summary>Shared options for JSON (de)serialization.</summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Lazy<HttpClient> _httpClient = new(() =>
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(CliConfig.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
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
            _log.LogWarning(ex, "Failed to load live market overview.");
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
            _log.LogWarning(ex, "Failed to load trading analysis for {Symbol} in {Currency}.", metalSymbol, currency);
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
            _log.LogWarning(ex, "Failed to load price history for {Symbol} ({Currency}).", metalSymbol, currency);
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
                _log.LogWarning("Request failed: {Endpoint} -> {StatusCode}", endpoint, response.StatusCode);
                throw new Exception($"Request failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var value = JsonSerializer.Deserialize<T>(json, DefaultJsonOptions)!;

            _cacheKeys.TryAdd(endpoint, 0);
            _cache.Set(endpoint, value, TimeSpan.FromMinutes(5));

            return value;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Request failed: {Endpoint}", endpoint);
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