using Elementum.Shared.DTOs;
using Elementum_Cli.Enums;
using Elementum_Cli.Constants;
using Elementum_Cli.Input;
using Elementum_Cli.Output;
using Elementum_Cli.Rendering;
using Elementum_Cli.Views.Interfaces;
using Elementum_Cli.Api;

namespace Elementum_Cli.Views;

public class HistoryView : IDetailView
{
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
            HistoryRenderer.RenderPeriodSelection(sym, name);
            return Task.CompletedTask;
        }

        var period = _selectedPeriod.Value;
        var count = PeriodCounts[period];
        var agg = period.ToString().ToLowerInvariant();


        CliOutputHelper.RenderViewHeader($"{CliStrings.HistoryHeaderPrefix}{name.ToUpperInvariant()} ({sym}) · {PeriodLabels[period]}");
        Console.WriteLine($"  Loading {PeriodLabels[period].ToLower()} ({count} values)…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderChartsAsync(sym, name, period, count, agg);
        return Task.CompletedTask;
    }

    private static async Task LoadAndRenderChartsAsync(string sym, string name, HistoryPeriod period, int count, string aggregation)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            List<PriceHistoryDto>? list = await HttpCall.GetPriceHistoryMetalWithLogicAsync(sym, aggregation, count, period);
            var periodLabel = PeriodLabels[period];
            Console.Clear();

            if (list == null || list.Count == 0)
            {
                HistoryRenderer.RenderNoDataMessage();
                CliOutputHelper.RenderViewFooter();
                return;
            }

            // API already returns at most count entries (aggregated); order by date for chart display.
            var ordered = list!.OrderBy(p => p.EntryDate).ToList();
            HistoryRenderer.RenderCharts(sym, name, periodLabel, ordered);
        });
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        var app = AppContext.Current!;
        if (!app.CurrentSelectedMetal.HasValue)
        {
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
                _selectedPeriod = null;
            HandleInputHelper.HandleInputWithMetals(key, () => RenderAsync());
            return;
        }

        if (!_selectedPeriod.HasValue)
        {
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
