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

    private static int GetHour(PriceHistoryDto p)
    {
        if (long.TryParse(p.ReferenceTimestamp, out long unix) && unix > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unix).ToLocalTime().Hour;
        }
        return 12;
    }

    private static List<PriceHistoryDto> AggregateByDay(List<PriceHistoryDto> list, int maxCount)
    {
        // Hourly progression of 1 day (the latest available day in history)
        if (list.Count == 0) return [];
        var latestDate = list.Max(p => p.EntryDate);

        return list
            .Where(p => p.EntryDate == latestDate)
            .OrderBy(p => p.Id)
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> AggregateByWeek(List<PriceHistoryDto> list, int maxCount)
    {
        // Last 7 days: Morning (<11h), Midday (11h-16h), Evening (>=17h) per day
        if (list.Count == 0) return [];
        var latestDate = list.Max(p => p.EntryDate);
        var weekStartDate = latestDate.AddDays(-6);

        var weekDays = list
            .Where(p => p.EntryDate >= weekStartDate && p.EntryDate <= latestDate)
            .GroupBy(p => p.EntryDate)
            .OrderBy(g => g.Key);

        var result = new List<PriceHistoryDto>();

        foreach (var dayGroup in weekDays)
        {
            var dayTicks = dayGroup.OrderBy(p => p.Id).ToList();
            if (dayTicks.Count == 0) continue;

            if (dayTicks.Count <= 3)
            {
                result.AddRange(dayTicks);
            }
            else
            {
                // 1. Morning (< 11:00 or first tick)
                var morning = dayTicks.FirstOrDefault(p => GetHour(p) < 11) ?? dayTicks.First();
                result.Add(morning);

                // 2. Midday (11:00 - 16:00)
                var midday = dayTicks.FirstOrDefault(p => GetHour(p) is >= 11 and <= 16);
                if (midday != null && midday.Id != morning.Id)
                {
                    result.Add(midday);
                }

                // 3. Evening (>= 17:00 or last tick)
                var evening = dayTicks.Last();
                if (evening.Id != morning.Id && (midday == null || evening.Id != midday.Id))
                {
                    result.Add(evening);
                }
            }
        }

        return result.TakeLast(maxCount).ToList();
    }

    private static List<PriceHistoryDto> AggregateByMonth(List<PriceHistoryDto> list, int maxCount)
    {
        // 1 value per day (end of the day / last tick of each day) for the last 30 days
        if (list.Count == 0) return [];
        var latestDate = list.Max(p => p.EntryDate);
        var startDate = latestDate.AddDays(-29);

        return list
            .Where(p => p.EntryDate >= startDate)
            .GroupBy(p => p.EntryDate)
            .OrderBy(g => g.Key)
            .Select(g => g.OrderByDescending(p => p.Id).First())
            .TakeLast(maxCount)
            .ToList();
    }

    private static List<PriceHistoryDto> AggregateByYear(List<PriceHistoryDto> list, int maxCount)
    {
        // 1 value per month: monthly average for up to 12 months
        return list
            .GroupBy(p => new { p.EntryDate.Year, p.EntryDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var avg = Math.Round(g.Average(p => p.Price), 2, MidpointRounding.ToEven);
                var rep = g.OrderByDescending(p => p.Id).First();
                return rep with { Price = avg };
            })
            .TakeLast(maxCount)
            .ToList();
    }
}