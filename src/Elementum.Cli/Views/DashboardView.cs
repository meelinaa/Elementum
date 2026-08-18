using Elementum.Application.DTOs;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>
/// Displays the current market overview: latest price per metal in a table (ID, Name, Price USD, Price EUR, Changes % since start of day).
/// </summary>
public class DashboardView : AsyncDetailViewBase
{
    /// <inheritdoc />
    protected override string ViewTitle => CliStrings.DashboardTitle;

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading live market data…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var overview = await HttpCall.GetLiveMarketOverviewAsync();

            var w = CliConstants.DashboardColumnWidths;

            Console.Clear();
            CliOutputHelper.RenderViewHeader(ViewTitle);

            Console.WriteLine();
            Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.DashboardTablePrefix, w));
            Console.WriteLine(TableFormatter.BuildRow(CliConstants.DashboardTablePrefix, w, new[] { "ID", "NAME", "PRICE USD", "PRICE EUR", "CHANGES (Chp)" }));
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

                    Console.Write(CliConstants.DashboardTablePrefix + "│ " + id + " │ " + name + " │ " + priceUsd + " │ " + priceEur + " │ ");
                    CliOutputHelper.WriteColoredValue(chpText, (item.ChpEur ?? 0) >= 0);
                    Console.WriteLine(" │");
                }
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
            }

            Console.WriteLine();
            DateTime? lastUpdate = overview?.LastUpdatedAtLocal;
            CliOutputHelper.RenderViewFooter(lastUpdate);
        });
    }
}
