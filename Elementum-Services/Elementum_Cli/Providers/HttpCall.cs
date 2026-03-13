using Elementum.Shared.Objects;
using Elementum_Cli.Config;
using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Elementum_Cli.Providers;

public class HttpCall
{
    private static readonly ILogger _log = CliLogging.GetLogger(nameof(HttpCall));

    /// <summary>Shared options for JSON (de)serialization. Use this everywhere to avoid repeated creation and ensure consistent behavior.</summary>
    public static readonly JsonSerializerOptions DefaultJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    private static readonly Lazy<HttpClient> _httpClient = new Lazy<HttpClient>(() => new HttpClient
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

    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol)
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}");
    }

    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string firstDate, string lastDate) // use this date format: yyyy-MM-dd
    {
        return await SendRequestAsync<string>($"history/{metalSymbol}/{firstDate}/{lastDate}");
    }

    // TODO: API suggestion: GET history/{symbol}/latest?count={n} ? returns the latest n entries for the metal, as JSON array. Use this for the "latest history" view.

    /// <summary>
    /// History with aggregation (daily/weekly/monthly/yearly).
    /// GET history/{symbol}/aggregated/{aggregation}/{count} (e.g. history/XAU/aggregated/monthly/12).
    /// </summary>
    public static async Task<string> GetPriceHistoryMetalAsync(string metalSymbol, string aggregation, int count)
    {
        var aggregationSegment = aggregation.Trim().ToLowerInvariant();
        return await SendRequestAsync<string>($"history/{metalSymbol}/aggregated={aggregationSegment}&{count}");
    }

    private static async Task<T> SendRequestAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.Value.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Request failed: {Endpoint} -> {StatusCode}", endpoint, response.StatusCode);
                throw new Exception($"Request failed: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

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

    public static async Task<PriceHistory?> GetPriceHistoryTodayWithLogicAsync(string sym, string name, string viewHeader)
    {
        var json = await HttpCall.GetPriceHistoryTodayAsync(sym);

        PriceHistory? ph = null;
        try
        {
            var list = JsonSerializer.Deserialize<List<PriceHistory>>(json, DefaultJsonOptions);
            ph = list?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Deserialize as list failed, trying single object for {Sym}", sym);
            try
            {
                ph = JsonSerializer.Deserialize<PriceHistory>(json, DefaultJsonOptions);
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

    public static async Task<List<PriceHistory>?> GetPriceHistoryMetalWithLogicAsync(string sym, string aggregation, int count, HistoryPeriod period)
    {
        List<PriceHistory>? list = null;
        try
        {
            var json = await HttpCall.GetPriceHistoryMetalAsync(sym, aggregation, count);
            try
            {
                list = JsonSerializer.Deserialize<List<PriceHistory>>(json, DefaultJsonOptions);
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Deserialize as list failed for {Sym} aggregated, trying single", sym);
                var single = JsonSerializer.Deserialize<PriceHistory>(json, DefaultJsonOptions);
                list = single != null ? new List<PriceHistory> { single } : null;
            }
        }
        catch (Exception ex)
        {
            _log.LogInformation(ex, "Aggregated history failed for {Sym}, falling back to full history", sym);
            try
            {
                var json = await HttpCall.GetPriceHistoryMetalAsync(sym);
                list = JsonSerializer.Deserialize<List<PriceHistory>>(json, DefaultJsonOptions);
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

    /// <summary>Client-side aggregation when API history/.../aggregated is not yet available.</summary>
    private static List<PriceHistory> AggregateClientSide(List<PriceHistory> ordered, HistoryPeriod period, int targetCount)
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

    private static List<PriceHistory> TakeEveryNth(List<PriceHistory> list, int step, int maxCount)
    {
        var result = new List<PriceHistory>();
        for (int i = list.Count - 1; i >= 0 && result.Count < maxCount; i -= step)
            result.Insert(0, list[i]);
        return result;
    }

    private static List<PriceHistory> TakeByMonth(List<PriceHistory> list, int maxCount)
    {
        var byMonth = list
            .GroupBy(p => (p.EntryDate.Year, p.EntryDate.Month))
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => g.OrderByDescending(p => p.EntryDate).First())
            .TakeLast(maxCount)
            .ToList();
        return byMonth;
    }

    private static List<PriceHistory> TakeByYear(List<PriceHistory> list, int maxCount)
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