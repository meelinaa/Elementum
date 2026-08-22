using Elementum.Application.DTOs;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;

namespace Elementum.Cli.Rendering;

/// <summary>
/// Dedicated renderer for the CLI Dashboard view: formats and prints the live precious metals market table.
/// Separates rendering logic and console formatting from async view lifecycle handling.
/// </summary>
public static class DashboardRenderer
{
    public static void RenderDashboard(LiveMarketOverviewDto? overview, string title)
    {
        var w = CliConstants.DashboardColumnWidths;

        Console.Clear();
        CliOutputHelper.RenderViewHeader(title);

        Console.WriteLine();
        Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.DashboardTablePrefix, w));
        Console.WriteLine(TableFormatter.BuildRow(CliConstants.DashboardTablePrefix, w, ["ID", "NAME", "PRICE USD", "PRICE EUR", "CHANGES (Chp)"]));
        Console.WriteLine(TableFormatter.BuildMidBorder(CliConstants.DashboardTablePrefix, w));

        if (overview == null || overview.Items.Count == 0)
        {
            Console.WriteLine(TableFormatter.BuildEmptyRow(CliConstants.DashboardTablePrefix, w, CliOutputHelper.NoMetalsFoundMessage));
            Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
        }
        else
        {
            foreach (var item in overview.Items)
            {
                string id = (item.Symbol ?? "").PadRight(w[0]);
                string name = (item.Name ?? "").PadRight(w[1]);
                string priceUsd = item.PriceUsd.ToString("N2") + " $";
                string priceEur = item.PriceEur.ToString("N2") + " €";
                string chpText = (item.ChpEur.HasValue ? (item.ChpEur.Value >= 0 ? "+" : "") + item.ChpEur.Value.ToString("0.00") : "0.00") + " %";

                priceUsd = priceUsd.PadLeft(w[2]);
                priceEur = priceEur.PadLeft(w[3]);
                chpText = chpText.PadLeft(w[4]);

                Console.Write($"{CliConstants.DashboardTablePrefix}│ {id} │ {name} │ {priceUsd} │ {priceEur} │ ");
                bool isPositive = (item.ChpEur ?? 0) >= 0;
                CliOutputHelper.WriteColoredValue(chpText, isPositive);
                Console.WriteLine(" │");
            }
            Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
        }

        Console.WriteLine();
        if (overview != null && overview.ExchangeRateUsdEur > 0)
        {
            Console.WriteLine($"  Exchange rate USD/EUR: {overview.ExchangeRateUsdEur:0.0000} (Source: Edelmetalle-API)");
            Console.WriteLine();
        }

        CliOutputHelper.RenderViewFooter();
    }
}
