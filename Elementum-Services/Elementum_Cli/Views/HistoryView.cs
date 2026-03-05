using System.Linq;
using Elementum.Shared.Objects;
using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Views;

public class HistoryView : IDetailView
{
    private const int BarChartHeight = 8;

    /// <summary>Number of data points per aggregation (passed to API as count).</summary>
    private static readonly Dictionary<HistoryPeriod, int> PeriodCounts = new()
    {
        { HistoryPeriod.Daily, 31 },
        { HistoryPeriod.Weekly, 52 },
        { HistoryPeriod.Monthly, 12 },
        { HistoryPeriod.Yearly, 10 }
    };

    private static readonly Dictionary<HistoryPeriod, string> PeriodLabels = new()
    {
        { HistoryPeriod.Daily, "Daily" },
        { HistoryPeriod.Weekly, "Weekly" },
        { HistoryPeriod.Monthly, "Monthly" },
        { HistoryPeriod.Yearly, "Yearly" }
    };

    private static HistoryPeriod? _selectedPeriod;

    public void Render()
    {
        Console.Clear();
        if (!currentSelectedMetal.HasValue)
        {
            CliOutputHelper.RenderMetalSelectionPrompt();
            return;
        }

        var sym = MetallHelper.GetSymbol(currentSelectedMetal.Value);
        var name = MetallHelper.GetName(currentSelectedMetal.Value);

        if (!_selectedPeriod.HasValue)
        {
            RenderPeriodSelection(sym, name);
            return;
        }

        var period = _selectedPeriod.Value;
        var count = PeriodCounts[period];
        var agg = period.ToString().ToLowerInvariant();

        CliOutputHelper.RenderViewHeader($"HISTORY — {name.ToUpperInvariant()} ({sym}) · {PeriodLabels[period]}");
        Console.WriteLine($"  Loading {PeriodLabels[period].ToLower()} ({count} values)…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderChartsAsync(sym, name, period, count, agg);
    }

    private static void RenderPeriodSelection(string sym, string name)
    {
        CliOutputHelper.RenderViewHeader($"HISTORY — {name.ToUpperInvariant()} ({sym})");
        CliOutputHelper.RenderSectionTitle("Select view");
        Console.WriteLine("  │  [1] Daily       — last 30 entries (day by day)                  │");
        Console.WriteLine("  │  [2] Weekly      — 52 values (per week)                          │");
        Console.WriteLine("  │  [3] Monthly     — 12 values (per month)                         │");
        Console.WriteLine("  │  [4] Yearly      — 10 values (per year)                          │");
        Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("  Press 1–4 to select. [ESC] Back to metal selection.");
        CliOutputHelper.RenderViewFooter();
    }

    private static async Task LoadAndRenderChartsAsync(string sym, string name, HistoryPeriod period, int count, string aggregation)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            List<PriceHistory>? list = null;

            try
            {
                var json = await HttpCall.GetPriceHistoryMetalAsync(sym, aggregation, count);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                try
                {
                    list = JsonSerializer.Deserialize<List<PriceHistory>>(json, options);
                }
                catch
                {
                    var single = JsonSerializer.Deserialize<PriceHistory>(json, options);
                    list = single != null ? new List<PriceHistory> { single } : null;
                }
            }
            catch
            {
                // Fallback: fetch full history and aggregate client-side (until API offers aggregated)
                try
                {
                    var json = await HttpCall.GetPriceHistoryMetalAsync(sym);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    list = JsonSerializer.Deserialize<List<PriceHistory>>(json, options);
                    if (list != null && list.Count > 0)
                        list = AggregateClientSide(list, period, count);
                }
                catch
                {
                    list = null;
                }
            }

            Console.Clear();
            CliOutputHelper.RenderViewHeader($"HISTORY — {name.ToUpperInvariant()} ({sym}) · {PeriodLabels[period]}");

            if (list == null || list.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("  No history data available for this metal.");
                Console.WriteLine("  Tip: Implement API endpoint history/{symbol}/aggregated?aggregation=&count=.");
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var ordered = list.OrderBy(p => p.EntryDate).ToList();
            var slice = ordered.TakeLast(count).ToList();

            var prices = slice.Select(p => (double)p.Price).ToArray();
            var chps = slice.Select(p => (double)(p.Chp ?? 0)).ToArray();
            var currentPrice = prices.Length > 0 ? prices[^1] : 0;

            CliOutputHelper.RenderSectionTitle($"Sparkline — {PeriodLabels[period]} (Chp)");
            ColoredSparkline(sym, currentPrice, chps, doubleWidth: true);

            CliOutputHelper.RenderSectionTitle("Price development (USD)");
            BarChart(sym, prices);

            Console.WriteLine();
            Console.WriteLine($"  Entries: {slice.Count} ({PeriodLabels[period].ToLower()})  ·  From {slice[0].EntryDate:yyyy-MM-dd} to {slice[^1].EntryDate:yyyy-MM-dd}");

            CliOutputHelper.RenderViewFooter();
        });
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

    private static void ColoredSparkline(string symbol, double currentPrice, double[] changes, bool doubleWidth = false)
    {
        Console.Write("  ");
        double currentChp = changes.Length > 0 ? changes[^1] : 0;
        Console.ForegroundColor = currentChp >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
        string spark = "";
        foreach (double ch in changes)
        {
            string block = ch > 0 ? "█" : ch < 0 ? "▁" : "─";
            spark += doubleWidth ? block + block : block;
        }
        Console.WriteLine($"{symbol.PadRight(6)} │ {currentPrice:N2} $ │ {spark}");
        Console.ResetColor();
    }

    private static void BarChart(string symbol, double[] values)
    {
        if (values.Length == 0) return;

        double maxVal = values.Max();
        if (maxVal <= 0) maxVal = 1;

        Console.WriteLine($"  {symbol.PadRight(6)} Price development:");
        Console.WriteLine();

        for (int row = BarChartHeight; row >= 0; row--)
        {
            string line = "       │";
            foreach (double v in values)
            {
                int barHeight = (int)Math.Round((v / maxVal) * BarChartHeight);
                string block = barHeight >= row ? "██" : "  ";
                line += block;
            }
            Console.WriteLine("  " + line);
        }
        Console.WriteLine("       └" + new string('─', values.Length * 2));
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        if (!currentSelectedMetal.HasValue)
        {
            var metall = MetallHelper.FromKey(key.KeyChar) ?? MetallHelper.FromConsoleKey(key.Key);
            if (metall.HasValue)
            {
                currentSelectedMetal = metall;
                Render();
            }
            else if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                currentState = AppState.Menu;
                currentDetailView = null;
                _selectedPeriod = null;
                CliOutputHelper.RenderMenu();
            }
            return;
        }

        if (!_selectedPeriod.HasValue)
        {
            // Period selection: 1–4
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                currentSelectedMetal = null;
                _selectedPeriod = null;
                Render();
                return;
            }
            HistoryPeriod? period = (key.KeyChar, key.Key) switch
            {
                ('1', _) or (_, ConsoleKey.D1) or (_, ConsoleKey.NumPad1) => HistoryPeriod.Daily,
                ('2', _) or (_, ConsoleKey.D2) or (_, ConsoleKey.NumPad2) => HistoryPeriod.Weekly,
                ('3', _) or (_, ConsoleKey.D3) or (_, ConsoleKey.NumPad3) => HistoryPeriod.Monthly,
                ('4', _) or (_, ConsoleKey.D4) or (_, ConsoleKey.NumPad4) => HistoryPeriod.Yearly,
                _ => null
            };
            if (period.HasValue)
            {
                _selectedPeriod = period.Value;
                Render();
            }
            return;
        }

        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
        {
            currentState = AppState.Menu;
            currentDetailView = null;
            currentSelectedMetal = null;
            _selectedPeriod = null;
            CliOutputHelper.RenderMenu();
        }
    }
}
