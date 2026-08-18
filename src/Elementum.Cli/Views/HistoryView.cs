using Elementum.Application.DTOs;
using Elementum.Cli.Aggregation;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Enums;
using Elementum.Cli.Input;
using Elementum.Cli.Output;
using Elementum.Cli.Rendering;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>
/// Two-step view: first select metal, then select period (Daily/Weekly/Monthly/Yearly). Renders sparkline and bar chart from aggregated history.
/// </summary>
public class HistoryView : IDetailView
{
    /// <summary>Number of data points per aggregation (passed to API as count).</summary>
    private static readonly Dictionary<HistoryPeriod, int> PeriodCounts = new()
    {
        { HistoryPeriod.Daily, 24 },
        { HistoryPeriod.Weekly, 21 },
        { HistoryPeriod.Monthly, 30 },
        { HistoryPeriod.Yearly, 12 }
    };

    private static readonly Dictionary<HistoryPeriod, string> PeriodLabels = new()
    {
        { HistoryPeriod.Daily, "Daily" },
        { HistoryPeriod.Weekly, "Weekly" },
        { HistoryPeriod.Monthly, "Monthly" },
        { HistoryPeriod.Yearly, "Yearly" }
    };

    private static HistoryPeriod? _selectedPeriod;

    /// <inheritdoc />
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


        CliOutputHelper.RenderViewHeader($"{CliStrings.HistoryHeaderPrefix}{name.ToUpperInvariant()} ({sym}) · {PeriodLabels[period]}");
        Console.WriteLine($"  Loading {PeriodLabels[period].ToLower()} ({count} values)…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderChartsAsync(sym, name, period, count);
        return Task.CompletedTask;
    }

    /// <summary>Fetches history data, aggregates via <see cref="HistoryDataAggregator"/> and renders charts via <see cref="HistoryRenderer"/>.</summary>
    private static async Task LoadAndRenderChartsAsync(string sym, string name, HistoryPeriod period, int count)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var app = AppContext.Current!;
            var currency = app?.SelectedCurrency ?? "EUR";
            var currencySymbol = app?.CurrencySymbol ?? "€";

            var rawList = await HttpCall.GetPriceHistoryMetalAsync(sym, currency);
            var list = HistoryDataAggregator.Aggregate(rawList, period, count, currency);
            var periodLabel = PeriodLabels[period];
            Console.Clear();

            if (list == null || list.Count == 0)
            {
                HistoryRenderer.RenderNoDataMessage();
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var ordered = list.OrderBy(p => p.EntryDate).ToList();
            HistoryRenderer.RenderCharts(sym, name, periodLabel, ordered, currency, currencySymbol);
        });
    }

    /// <inheritdoc />
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
