using System.Collections.Concurrent;
using System.Globalization;
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

    /// <summary>GET history/{symbol}/candles — daily candle summaries for charts.</summary>
    public static async Task<List<DailyPriceSummaryDto>> GetDailyCandlesAsync(string metalSymbol, string currency = "EUR")
    {
        try
        {
            return await SendRequestAsync<List<DailyPriceSummaryDto>>($"history/{metalSymbol}/candles?currency={currency}");
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load candles for {Symbol}.", metalSymbol);
            return [];
        }
    }

    /// <summary>GET history/{symbol}?currency={currency} — returns JSON array of price history for one metal and currency.</summary>
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

    /// <summary>Fetches history data for charts with currency filtering and gap-ignoring aggregation logic.</summary>
    public static async Task<List<PriceHistoryDto>?> GetPriceHistoryMetalWithLogicAsync(string sym, string aggregation, int count, HistoryPeriod period, string currency = "EUR")
    {
        try
        {
            var rawList = await GetPriceHistoryMetalAsync(sym, currency);
            if (rawList != null && rawList.Count > 0)
            {
                // Strict currency filter
                var filtered = rawList
                    .Where(p => string.Equals(p.Currency, currency, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.EntryDate)
                    .ThenBy(p => p.Id)
                    .ToList();

                if (filtered.Count > 0)
                {
                    return AggregateClientSide(filtered, period, count);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to load history for {Sym}.", sym);
        }

        return null;
    }

    private static List<PriceHistoryDto> AggregateClientSide(List<PriceHistoryDto> list, HistoryPeriod period, int targetCount)
    {
        var ordered = list.OrderBy(p => p.EntryDate).ThenBy(p => p.Id).ToList();

        return period switch
        {
            HistoryPeriod.Daily => AggregateByDay(ordered, targetCount),
            HistoryPeriod.Weekly => AggregateByWeek(ordered, targetCount),
            HistoryPeriod.Monthly => AggregateByMonth(ordered, targetCount),
            HistoryPeriod.Yearly => AggregateByYear(ordered, targetCount),
            _ => ordered.TakeLast(targetCount).ToList()
        };
    }

    private static List<PriceHistoryDto> AggregateByDay(List<PriceHistoryDto> list, int maxCount)
    {
        // 1 entry per distinct day (latest tick of each day)
        return list
            .GroupBy(p => p.EntryDate)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderByDescending(p => p.Id).First())
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> AggregateByWeek(List<PriceHistoryDto> list, int maxCount)
    {
        // 1 entry per distinct calendar week
        return list
            .GroupBy(p => new { p.EntryDate.Year, Week = ISOWeek.GetWeekOfYear(p.EntryDate.ToDateTime(TimeOnly.MinValue)) })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
            .Select(g => g.OrderByDescending(p => p.EntryDate).ThenByDescending(p => p.Id).First())
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> AggregateByMonth(List<PriceHistoryDto> list, int maxCount)
    {
        // 1 entry per distinct month (up to 24 months / 2 years)
        return list
            .GroupBy(p => new { p.EntryDate.Year, p.EntryDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.OrderByDescending(p => p.EntryDate).ThenByDescending(p => p.Id).First())
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> AggregateByYear(List<PriceHistoryDto> list, int maxCount)
    {
        // For each year: January (or start of year) and Mid-Year (June/July or mid of year)
        var result = new List<PriceHistoryDto>();

        var yearGroups = list
            .GroupBy(p => p.EntryDate.Year)
            .OrderBy(g => g.Key);

        foreach (var yg in yearGroups)
        {
            var orderedYear = yg.OrderBy(p => p.EntryDate).ThenBy(p => p.Id).ToList();

            // 1. January / H1 point (earliest entry in the first half of the year)
            var janPoint = orderedYear.FirstOrDefault(p => p.EntryDate.Month <= 5) ?? orderedYear.First();
            result.Add(janPoint);

            // 2. Mid-Year / H2 point (e.g. June/July or earliest in second half, if distinct)
            var midYearPoint = orderedYear.FirstOrDefault(p => p.EntryDate.Month >= 6);
            if (midYearPoint != null && midYearPoint.Id != janPoint.Id && midYearPoint.EntryDate != janPoint.EntryDate)
            {
                result.Add(midYearPoint);
            }
        }

        return result.TakeLast(maxCount).ToList();
    }
}