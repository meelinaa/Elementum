using Elementum.Shared.Objects;
using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;

namespace Elementum_Cli.Views;

public class HistoryView : IDetailView
{
    private const int BarChartHeight = 8;

    /// <summary>Number of data points per aggregation (passed to API as count).</summary>
    private static readonly Dictionary<HistoryPeriod, int> PeriodCounts = new() // These are somewhat arbitrary and can be adjusted based on how much data we want to show for each period, and also based on typical API limits for aggregated data (e.g. if API offers history/{symbol}/aggregated?aggregation=weekly&count=52, then 52 weeks = 1 year of weekly data, which seems reasonable for a historical view)
    {
        { HistoryPeriod.Daily, 31 },
        { HistoryPeriod.Weekly, 52 },
        { HistoryPeriod.Monthly, 12 },
        { HistoryPeriod.Yearly, 10 }
    };

    private static readonly Dictionary<HistoryPeriod, string> PeriodLabels = new() // These are the display labels for the periods, used in the UI
    {
        { HistoryPeriod.Daily, "Daily" },
        { HistoryPeriod.Weekly, "Weekly" },
        { HistoryPeriod.Monthly, "Monthly" },
        { HistoryPeriod.Yearly, "Yearly" }
    };

    private static HistoryPeriod? _selectedPeriod;

    public Task RenderAsync()
    {
        var app = AppContext.Current!;
        Console.Clear();
        if (!app.CurrentSelectedMetal.HasValue)
        {
            CliOutputHelper.RenderMetalSelectionPrompt();
            return Task.CompletedTask;
        }

        var sym = MetallHelper.GetSymbol(app.CurrentSelectedMetal.Value);
        var name = MetallHelper.GetName(app.CurrentSelectedMetal.Value);

        if (!_selectedPeriod.HasValue)
        {
            RenderPeriodSelection(sym, name);
            return Task.CompletedTask;
        }

        var period = _selectedPeriod.Value;
        var count = PeriodCounts[period];
        var agg = period.ToString().ToLowerInvariant();

        CliOutputHelper.RenderViewHeader($"HISTORY — {name.ToUpperInvariant()} ({sym}) · {PeriodLabels[period]}");
        Console.WriteLine($"  Loading {PeriodLabels[period].ToLower()} ({count} values)…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderChartsAsync(sym, name, period, count, agg);
        return Task.CompletedTask;
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

    // This method tries to fetch aggregated data from the API first, and if that fails (e.g. endpoint not implemented), it falls back to fetching full history and doing client-side aggregation.
    // This way we can show some historical data even if the API doesn't yet support aggregated endpoints, while still benefiting from more efficient aggregated data when available.
    private static async Task LoadAndRenderChartsAsync(string sym, string name, HistoryPeriod period, int count, string aggregation)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            List<PriceHistory>? list = await HttpCall.GetPriceHistoryMetalWithLogicAsync(sym, aggregation, count, period);

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

    private static void ColoredSparkline(string symbol, double currentPrice, double[] changes, bool doubleWidth = false)
    {
        Console.Write("  ");
        double currentChp = changes.Length > 0 ? changes[^1] : 0;
        string spark = "";
        foreach (double ch in changes)
        {
            string block = ch > 0 ? "█" : ch < 0 ? "▁" : "─";
            spark += doubleWidth ? block + block : block;
        }
        CliOutputHelper.WriteColoredValue($"{symbol.PadRight(6)} │ {currentPrice:N2} $ │ {spark}\n", currentChp >= 0);
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
        var app = AppContext.Current!;
        // Delegate metal selection (and ESC back to menu) to helper; clear period when leaving
        if (!app.CurrentSelectedMetal.HasValue)
        {
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
                _selectedPeriod = null;
            HandleInputHelper.HandleInputWithMetals(key, () => RenderAsync());
            return;
        }

        if (!_selectedPeriod.HasValue)
        {
            // Period selection: 1–4
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                app.CurrentSelectedMetal = null;
                _selectedPeriod = null;
                _ = RenderAsync();
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
                _ = RenderAsync();
            }
            return;
        }

        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
        {
            app.State = AppState.Menu;
            app.CurrentDetailView = null;
            app.CurrentSelectedMetal = null;
            _selectedPeriod = null;
            CliOutputHelper.RenderMenu();
        }
    }
}
