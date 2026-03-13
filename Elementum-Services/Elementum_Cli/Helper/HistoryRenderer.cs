using Elementum.Shared.DTOs;
using Elementum_Cli.Enums;

namespace Elementum_Cli.Helper;

/// <summary>Renders the History view: period selection screen, sparkline, and bar chart. Extracted for readability and testability.</summary>
public static class HistoryRenderer
{
    private const int BarChartHeight = 8;

    /// <summary>Renders the period selection screen (Daily/Weekly/Monthly/Yearly).</summary>
    public static void RenderPeriodSelection(string sym, string name)
    {
        CliOutputHelper.RenderViewHeader($"{CliStrings.HistoryHeaderPrefix}{name.ToUpperInvariant()} ({sym})");
        CliOutputHelper.RenderSectionTitle(CliStrings.HistorySelectViewTitle);
        Console.WriteLine(CliStrings.HistoryOptionDaily);
        Console.WriteLine(CliStrings.HistoryOptionWeekly);
        Console.WriteLine(CliStrings.HistoryOptionMonthly);
        Console.WriteLine(CliStrings.HistoryOptionYearly);
        Console.WriteLine(CliStrings.HistoryPeriodBoxBottom);
        Console.WriteLine();
        Console.WriteLine("  " + CliStrings.PressToSelectHistoryEscBack);
        CliOutputHelper.RenderViewFooter();
    }

    /// <summary>Renders the "no data" message lines (caller draws header and footer).</summary>
    public static void RenderNoDataMessage()
    {
        Console.WriteLine();
        Console.WriteLine("  " + CliStrings.HistoryNoDataMessage);
        Console.WriteLine("  " + CliStrings.HistoryNoDataTip);
    }

    /// <summary>Renders the full charts view: header, sparkline, bar chart, entries summary, footer.</summary>
    public static void RenderCharts(string sym, string name, string periodLabel, IReadOnlyList<PriceHistoryDto> slice)
    {
        CliOutputHelper.RenderViewHeader($"{CliStrings.HistoryHeaderPrefix}{name.ToUpperInvariant()} ({sym}) · {periodLabel}");

        var prices = slice.Select(p => (double)p.Price).ToArray();
        var chps = slice.Select(p => (double)(p.Chp ?? 0)).ToArray();
        var currentPrice = prices.Length > 0 ? prices[^1] : 0;

        CliOutputHelper.RenderSectionTitle(string.Format(CliStrings.HistorySparklineSectionTitle, periodLabel));
        RenderSparkline(sym, currentPrice, chps, doubleWidth: true);

        CliOutputHelper.RenderSectionTitle(CliStrings.HistoryPriceDevelopmentSectionTitle);
        RenderBarChart(sym, prices);

        Console.WriteLine();
        Console.WriteLine(string.Format(CliStrings.HistoryEntriesSummary, slice.Count, periodLabel.ToLowerInvariant(), slice[0].EntryDate, slice[^1].EntryDate));

        CliOutputHelper.RenderViewFooter();
    }

    /// <summary>Draws a colored sparkline (Chp) with current price.</summary>
    public static void RenderSparkline(string symbol, double currentPrice, double[] changes, bool doubleWidth = false)
    {
        Console.Write("  ");
        double currentChp = changes.Length > 0 ? changes[^1] : 0;
        var spark = BuildSparkline(changes, doubleWidth);
        CliOutputHelper.WriteColoredValue($"{symbol.PadRight(6)} │ {currentPrice:N2} $ │ {spark}\n", currentChp >= 0);
    }

    /// <summary>Builds the sparkline character sequence (for testing without console).</summary>
    public static string BuildSparkline(double[] changes, bool doubleWidth = false)
    {
        string spark = "";
        foreach (double ch in changes)
        {
            string block = ch > 0 ? "█" : ch < 0 ? "▁" : "─";
            spark += doubleWidth ? block + block : block;
        }
        return spark;
    }

    /// <summary>Draws a simple ASCII bar chart for price values.</summary>
    public static void RenderBarChart(string symbol, double[] values)
    {
        if (values.Length == 0) return;

        double maxVal = values.Max();
        if (maxVal <= 0) maxVal = 1;

        Console.WriteLine($"  {symbol.PadRight(6)} {CliStrings.HistoryPriceDevelopmentLabel}");
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
}
