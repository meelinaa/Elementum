using System.Globalization;
using System.Text;
using Elementum.Application.DTOs;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;

namespace Elementum.Cli.Rendering;

/// <summary>Renders the History view: period selection screen, Chp sparkline, and price bar chart.</summary>
public static class HistoryRenderer
{
    private const int BarChartHeight = 8;

    /// <summary>Width for right-aligned Y-axis tick labels (price).</summary>
    private const int YAxisLabelWidth = 10;

    /// <summary>
    /// Outer box width for History charts.
    /// </summary>
    public static int ComputeHistoryViewWidth(int entryCount)
    {
        if (entryCount <= 0)
            return CliConstants.ViewWidth;

        int chartRowWidth = 2 + YAxisLabelWidth + 3 + entryCount * 2;
        const int sparkPriceMaxChars = 16;
        int sparkRowWidth = 2 + 6 + 3 + sparkPriceMaxChars + 5 + entryCount * 2;

        int content = Math.Max(chartRowWidth, sparkRowWidth);
        return Math.Max(CliConstants.ViewWidth, content);
    }

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

    /// <summary>Renders the "no data" message lines.</summary>
    public static void RenderNoDataMessage()
    {
        Console.WriteLine();
        Console.WriteLine("  " + CliStrings.HistoryNoDataMessage);
        Console.WriteLine("  " + CliStrings.HistoryNoDataTip);
    }

    /// <summary>Renders the full charts view: header, sparkline, bar chart, entries summary, footer.</summary>
    public static void RenderCharts(string sym, string name, string periodLabel, IReadOnlyList<PriceHistoryDto> slice, string currency = "EUR", string currencySymbol = "€")
    {
        int boxWidth = ComputeHistoryViewWidth(slice.Count);
        CliOutputHelper.RenderViewHeader($"{CliStrings.HistoryHeaderPrefix}{name.ToUpperInvariant()} ({sym}) · {periodLabel}", boxWidth);

        var prices = slice.Select(p => (double)p.Price).ToArray();
        var chps = slice.Select(p => (double)(p.Chp ?? 0)).ToArray();
        var currentPrice = prices.Length > 0 ? prices[^1] : 0;

        CliOutputHelper.RenderSectionTitle(string.Format(CliStrings.HistorySparklineSectionTitle, periodLabel), boxWidth);
        RenderSparkline(sym, currentPrice, chps, currencySymbol, doubleWidth: true);

        CliOutputHelper.RenderSectionTitle(string.Format(CliStrings.HistoryPriceDevelopmentSectionTitle, currency), boxWidth);
        RenderBarChart(sym, prices);

        Console.WriteLine();
        if (slice.Count == 1)
        {
            Console.WriteLine($"  Entries: 1 ({periodLabel.ToLowerInvariant()})  ·  {slice[0].EntryDate:dd.MM.yyyy}");
        }
        else
        {
            Console.WriteLine(string.Format(CliStrings.HistoryEntriesSummary, slice.Count, periodLabel.ToLowerInvariant(), slice[0].EntryDate, slice[^1].EntryDate));
        }

        var lastUpdate = slice.Max(p => p.EntryDate);
        CliOutputHelper.RenderViewFooter(lastUpdate, boxWidth);
    }

    /// <summary>Draws the Chp sparkline.</summary>
    public static void RenderSparkline(string symbol, double currentPrice, double[] changes, string currencySymbol, bool doubleWidth)
    {
        var spark = BuildSparkline(changes, doubleWidth);
        Console.WriteLine();
        Console.WriteLine($"    {symbol,-6} │ {currentPrice:N2} {currencySymbol} │ {spark}");
    }

    /// <summary>Builds the sparkline character sequence.</summary>
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

    /// <summary>Draws a bar chart with Y-axis price ticks.</summary>
    public static void RenderBarChart(string symbol, double[] values)
    {
        if (values.Length == 0) return;

        double minVal = values.Min();
        double maxVal = values.Max();
        double range = maxVal - minVal;
        if (range < 1e-9)
        {
            double pad = Math.Abs(maxVal) * 0.002 + 0.01;
            minVal -= pad;
            maxVal += pad;
            range = maxVal - minVal;
        }

        int[] barHeights = values.Select(v =>
        {
            int h = (int)Math.Round((v - minVal) / range * BarChartHeight);
            return Math.Clamp(h, 0, BarChartHeight);
        }).ToArray();

        var inv = CultureInfo.InvariantCulture;

        for (int row = BarChartHeight; row >= 0; row--)
        {
            double gridPrice = minVal + range * row / BarChartHeight;
            string label;
            if (row == BarChartHeight || row == BarChartHeight / 2 || row == 0)
                label = gridPrice.ToString("N2", inv).PadLeft(YAxisLabelWidth);
            else
                label = new string(' ', YAxisLabelWidth);

            var line = new StringBuilder();
            line.Append("  ").Append(label).Append(" │");
            for (int i = 0; i < values.Length; i++)
            {
                string block = barHeights[i] >= row ? "██" : "  ";
                line.Append(block);
            }
            Console.WriteLine(line.ToString());
        }

        var bottom = new StringBuilder();
        bottom.Append("  ").Append(new string(' ', YAxisLabelWidth)).Append(" └──");
        bottom.Append(new string('─', values.Length * 2));
        Console.WriteLine(bottom.ToString());
    }
}
