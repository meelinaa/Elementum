using System.Collections.Concurrent;
using System.Text.Json;
using Elementum.Application.DTOs;
using Elementum.Cli.Config;
using Elementum.Cli.Enums;
using Elementum.Cli.Logging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Cli.Api;

/// <summary>
/// Central HTTP client for the CLI. Calls the Elementum REST API with 5-minute in-memory caching and clean fallback handling.
/// </summary>
public class HttpCall
{
    private static readonly ILogger _log = CliLogging.GetLogger(nameof(HttpCall));

    /// <summary>Shared options for JSON (de)serialization.</summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    private static readonly Lazy<HttpClient> _httpClient = new(() => new HttpClient
    {
        BaseAddress = new Uri(CliConfig.ApiBaseUrl)
    });

    /// <summary>GET prices/live — returns 5-minute live market overview for Dashboard.</summary>
    public static async Task<LiveMarketOverviewDto?> GetLiveMarketOverviewAsync()
    {
        try
        {
            return await SendRequestAsync<LiveMarketOverviewDto>("prices/live");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load live market overview from API.");
            return null;
        }
    }

    /// <summary>Calls GET history/{symbol}/latest/trading?currency={currency} and returns <see cref="TradingPriceDto"/> for TradingView.</summary>
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

    /// <summary>GET history/{symbol}/candles — daily candle summaries for charts.</summary>
    public static async Task<List<DailyPriceSummaryDto>?> GetDailyCandlesAsync(string metalSymbol, string currency = "EUR")
    {
        try
        {
            return await SendRequestAsync<List<DailyPriceSummaryDto>>($"history/{metalSymbol}/candles?currency={currency}");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load candles for {Symbol}.", metalSymbol);
            return null;
        }
    }

    /// <summary>GET history/{symbol} — returns JSON array of all price history for one metal.</summary>
    public static async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalAsync(string metalSymbol)
    {
        try
        {
            return await SendRequestAsync<List<PriceHistoryDto>>($"history/{metalSymbol}");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load price history for {Symbol}.", metalSymbol);
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

    /// <summary>Fetches history data for charts with logic.</summary>
    public static async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalWithLogicAsync(string sym, string aggregation, int count, HistoryPeriod period)
    {
        try
        {
            var rawList = await GetPriceHistoryMetalAsync(sym);
            if (rawList != null && rawList.Count > 0)
            {
                return AggregateClientSide(rawList, period, count);
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load history for {Sym}.", sym);
        }

        return null;
    }

    private static List<PriceHistoryDto> AggregateClientSide(List<PriceHistoryDto> ordered, HistoryPeriod period, int targetCount)
    {
        ordered = ordered.OrderBy(p => p.EntryDate).ToList();
        if (ordered.Count <= targetCount) return ordered.TakeLast(targetCount).ToList();

        return period switch
        {
            HistoryPeriod.Daily => ordered.TakeLast(targetCount).ToList(),
            HistoryPeriod.Weekly => TakeEveryNth(ordered, 7, targetCount),
            HistoryPeriod.Monthly => TakeByMonth(ordered, targetCount),
            HistoryPeriod.Yearly => TakeByYear(ordered, targetCount),
            _ => ordered.TakeLast(targetCount).ToList()
        };
    }

    private static List<PriceHistoryDto> TakeEveryNth(List<PriceHistoryDto> list, int step, int maxCount)
    {
        var result = new List<PriceHistoryDto>();
        for (int i = list.Count - 1; i >= 0 && result.Count < maxCount; i -= step)
            result.Insert(0, list[i]);
        return result;
    }

    private static List<PriceHistoryDto> TakeByMonth(List<PriceHistoryDto> list, int maxCount)
    {
        return list
            .GroupBy(p => (p.EntryDate.Year, p.EntryDate.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> TakeByYear(List<PriceHistoryDto> list, int maxCount)
    {
        return list
            .GroupBy(p => p.EntryDate.Year)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
    }
}