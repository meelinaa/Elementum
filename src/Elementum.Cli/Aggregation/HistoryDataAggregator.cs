using Elementum.Application.DTOs;
using Elementum.Cli.Enums;

namespace Elementum.Cli.Aggregation;

/// <summary>
/// Service responsible for aggregating and downsampling raw historical price records
/// into period-specific buckets (Daily, Weekly, Monthly, Yearly) for CLI charts.
/// </summary>
public static class HistoryDataAggregator
{
    /// <summary>
    /// Filters raw price history by currency and applies the selected temporal aggregation strategy.
    /// </summary>
    public static List<PriceHistoryDto>? Aggregate(
        IReadOnlyList<PriceHistoryDto>? rawList,
        HistoryPeriod period,
        int targetCount,
        string currency = "EUR")
    {
        if (rawList == null || rawList.Count == 0)
            return null;

        // Strict currency filter & chronological order
        var filtered = rawList
            .Where(p => string.Equals(p.Currency, currency, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.EntryDate)
            .ThenBy(p => p.Id)
            .ToList();

        if (filtered.Count == 0)
            return null;

        return period switch
        {
            HistoryPeriod.Daily => AggregateByDay(filtered, targetCount),
            HistoryPeriod.Weekly => AggregateByWeek(filtered, targetCount),
            HistoryPeriod.Monthly => AggregateByMonth(filtered, targetCount),
            HistoryPeriod.Yearly => AggregateByYear(filtered, targetCount),
            _ => filtered.TakeLast(targetCount).ToList()
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
