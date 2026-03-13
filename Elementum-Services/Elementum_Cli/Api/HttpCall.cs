using System.Collections.Concurrent;
using Elementum.Shared.DTOs;
using Elementum_Cli.Config;
using Elementum_Cli.Enums;
using Elementum_Cli.Logging;
using Elementum_Cli.Output;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Elementum_Cli.Api;

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

    public static async Task<string> GetMetalListAsync()
    {
        return await SendRequestAsync<string>("metals/all");
    }

    public static async Task<string> GetPriceHistoryAllTodayAsync()
    {
        return await SendRequestAsync<string>("history/all/latest");
    }

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

    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}");
    }

    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string firstDate, string lastDate) // use this date format: yyyy-MM-dd
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
        var byMonth = list
            .GroupBy(p => (p.EntryDate.Year, p.EntryDate.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
        return byMonth;
    }

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