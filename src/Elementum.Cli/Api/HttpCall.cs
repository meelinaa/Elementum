using System.Collections.Concurrent;
using Elementum.Application.DTOs;
using Elementum.Cli.Config;
using Elementum.Cli.Enums;
using Elementum.Cli.Logging;
using Elementum.Cli.Output;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Elementum.Cli.Api;

/// <summary>
/// Central HTTP client for the CLI. Calls the Elementum API (metals, price history, trading, karat, aggregated).
/// Responses are cached in memory per endpoint; use <see cref="ClearCache"/> to force a reload (e.g. [R] key).
/// </summary>
public class HttpCall
{
    private static readonly ILogger _log = CliLogging.GetLogger(nameof(HttpCall));

    /// <summary>Shared options for JSON (de)serialization. Use this everywhere to avoid repeated creation and ensure consistent behavior.</summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private static readonly ConcurrentDictionary<string, byte> _cacheKeys = new();

    private static readonly Lazy<HttpClient> _httpClient = new(() => new HttpClient
    {
        BaseAddress = new Uri(CliConfig.ApiBaseUrl)
    });

    /// <summary>GET metals/all — returns JSON array of all metals (Id, Symbol, Name).</summary>
    public static async Task<string> GetMetalListAsync()
    {
        return await SendRequestAsync<string>("metals/all");
    }

    /// <summary>GET history/all/latest — returns JSON array of latest price per metal (for Dashboard).</summary>
    public static async Task<string> GetPriceHistoryAllTodayAsync()
    {
        return await SendRequestAsync<string>("history/all/latest");
    }

    /// <summary>GET history/{symbol}/latest — returns JSON for the latest price of one metal.</summary>
    public static async Task<string> GetPriceHistoryTodayAsync(string metalSymbol)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}/latest");
    }

    /// <summary>Calls GET history/{symbol}/latest/trading and returns <see cref="TradingPriceDto"/> for TradingView.</summary>
    public static async Task<string> GetPriceHistoryTradingLatestAsync(string metalSymbol)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}/latest/trading");
    }

    /// <summary>Calls GET history/{symbol}/latest/karat and returns <see cref="KaratPricesDto"/> for KaratCalculatorView.</summary>
    public static async Task<KaratPricesDto?> GetPriceHistoryKaratLatestAsync(string metalSymbol)
    {
        try
        {
            var json = await SendRequestAsync<string>($"history/{metalSymbol}/latest/karat");
            return JsonSerializer.Deserialize<KaratPricesDto>(json, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load karat data for {Symbol}", metalSymbol);
            return null;
        }
    }

    /// <summary>GET history/{symbol} — returns JSON array of all price history for one metal.</summary>
    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}");
    }

    /// <summary>GET history/{symbol}/{firstDate}/{lastDate} — returns JSON array for date range. Dates in yyyy-MM-dd.</summary>
    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string firstDate, string lastDate)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}/{firstDate}/{lastDate}");
    }

    /// <summary>
    /// History with aggregation (daily/weekly/monthly/yearly).
    /// GET history/{symbol}/aggregated/{aggregation}/{count} (e.g. history/XAU/aggregated/monthly/12).
    /// </summary>
    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string aggregation, int count)
    {
        var aggregationSegment = aggregation.Trim().ToLowerInvariant();
        return await SendRequestAsync<string>($"history/{metalSymbol}/aggregated/{aggregationSegment}/{count}");
    }

    /// <summary>Sends GET to the given endpoint; returns cached JSON if present, otherwise fetches and caches. Deserializes to T when T is not string.</summary>
    private static async Task<T> SendRequestAsync<T>(string endpoint)
    {
        try
        {
            if (_cache.TryGetValue(endpoint, out string? cachedJson) && cachedJson != null)
            {
                if (typeof(T) == typeof(string))
                    return (T)(object)cachedJson;
                return JsonSerializer.Deserialize<T>(cachedJson, DefaultJsonOptions)!;
            }

            var response = await _httpClient.Value.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Request failed: {Endpoint} -> {StatusCode}", endpoint, response.StatusCode);
                throw new Exception($"Request failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            _cacheKeys.TryAdd(endpoint, 0);
            _cache.Set(endpoint, json, new MemoryCacheEntryOptions());

            if (typeof(T) == typeof(string))
                return (T)(object)json;

            return JsonSerializer.Deserialize<T>(json, DefaultJsonOptions)!;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Request failed: {Endpoint}", endpoint);
            throw;
        }
    }

    /// <summary>Clears all cached API responses (e.g. when implementing a manual "refresh" in views).</summary>
    public static void ClearCache()
    {
        foreach (var key in _cacheKeys.Keys)
            _cache.Remove(key);
        _cacheKeys.Clear();
    }

    /// <summary>Legacy: fetches history/{symbol}/latest and deserializes as TradingPriceDto (list or single). Prefer <see cref="GetPriceHistoryTradingLatestAsync"/> for TradingView.</summary>
    public static async Task<TradingPriceDto?> GetPriceHistoryTodayWithLogicAsync(string sym, string name, string viewHeader)
    {
        var json = await HttpCall.GetPriceHistoryTodayAsync(sym);

        TradingPriceDto? ph = null;
        try
        {
            var list = JsonSerializer.Deserialize<List<TradingPriceDto>>(json, DefaultJsonOptions);
            ph = list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Deserialize as list failed, trying single object for {Sym}", sym);
            try
            {
                ph = JsonSerializer.Deserialize<TradingPriceDto>(json, DefaultJsonOptions);
            }
            catch (Exception ex2)
            {
                _log.LogWarning(ex2, "Failed to deserialize price history for {Sym}", sym);
            }
        }

        Console.Clear();
        CliOutputHelper.RenderViewHeader($"{viewHeader} ? {name.ToUpperInvariant()} ({sym})");

        if (ph == null)
        {
            Console.WriteLine();
            Console.WriteLine("  " + CliOutputHelper.NoDataMessageForMetal);
            CliOutputHelper.RenderViewFooter();
        }

        return ph;
    }

    /// <summary>Fetches aggregated history (GET history/{symbol}/aggregated/{aggregation}/{count}); on failure falls back to full history and aggregates client-side. Returns deserialized list or null.</summary>
    public static async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalWithLogicAsync(string sym, string aggregation, int count, HistoryPeriod period)
    {
        List<PriceHistoryDto>? list = null;
        try
        {
            var json = await GetPriceHistoryMetalAsync(sym, aggregation, count);
            try
            {
                list = JsonSerializer.Deserialize<List<PriceHistoryDto>>(json, DefaultJsonOptions);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Deserialize as list failed for {Sym} aggregated, trying single", sym);
                var single = JsonSerializer.Deserialize<PriceHistoryDto>(json, DefaultJsonOptions);
                list = single != null ? [single] : null;
            }
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Aggregated history failed for {Sym}, falling back to full history", sym);
            try
            {
                var json = await GetPriceHistoryMetalAsync(sym);
                list = JsonSerializer.Deserialize<List<PriceHistoryDto>>(json, DefaultJsonOptions);
                if (list != null && list.Count > 0)
                    list = AggregateClientSide(list, period, count);
            }
            catch (Exception ex2)
            {
                _log.LogWarning(ex2, "Failed to load price history for {Sym}", sym);
                list = null;
            }
        }

        Console.Clear();

        return list;
    }

    /// <summary>Reduces the list to at most targetCount entries: by period (daily take last N, weekly/monthly/yearly by grouping).</summary>
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

    /// <summary>Takes every nth element from the end of the list, up to maxCount entries (chronological order).</summary>
    private static List<PriceHistoryDto> TakeEveryNth(List<PriceHistoryDto> list, int step, int maxCount)
    {
        var result = new List<PriceHistoryDto>();
        for (int i = list.Count - 1; i >= 0 && result.Count < maxCount; i -= step)
            result.Insert(0, list[i]);
        return result;
    }

    /// <summary>Groups by year/month, keeps last entry per month, returns last maxCount months.</summary>
    private static List<PriceHistoryDto> TakeByMonth(List<PriceHistoryDto> list, int maxCount)
    {
        var byMonth = list
            .GroupBy(p => (p.EntryDate.Year, p.EntryDate.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
        return byMonth;
    }

    /// <summary>Groups by year, keeps last entry per year, returns last maxCount years.</summary>
    private static List<PriceHistoryDto> TakeByYear(List<PriceHistoryDto> list, int maxCount)
    {
        var byYear = list
            .GroupBy(p => p.EntryDate.Year)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
        return byYear;
    }
}