using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;

namespace Elementum_Cli.Views;

public class DashboardView : AsyncDetailViewBase
{
    protected override string ViewTitle => "CURRENT MARKET OVERVIEW";
    protected override string LoadingMessage => "Loading market data…";

    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetPriceHistoryAllTodayAsync();
            var priceHistories = JsonSerializer.Deserialize<List<PriceHistory>>(json, HttpCall.DefaultJsonOptions);

            var w = CliConstants.DashboardColumnWidths;

            Console.Clear();
            CliOutputHelper.RenderViewHeader(ViewTitle);

            Console.WriteLine();
            Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.DashboardTablePrefix, w));
            Console.WriteLine(TableFormatter.BuildRow(CliConstants.DashboardTablePrefix, w, new[] { "ID", "NAME", "EXCHANGE", "PRICE USD", "CHANGES (Chp)" }));
            Console.WriteLine(TableFormatter.BuildMidBorder(CliConstants.DashboardTablePrefix, w));

            if (priceHistories == null || priceHistories.Count == 0)
            {
                Console.WriteLine(TableFormatter.BuildEmptyRow(CliConstants.DashboardTablePrefix, w, CliOutputHelper.NoMetalsFoundMessage));
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
            }
            else
            {
                foreach (var metal in priceHistories)
                {
                    string id = (metal.Metal?.Symbol ?? metal.Symbol ?? "").PadRight(w[0]);
                    string name = (metal.Metal?.Name ?? "").PadRight(w[1]);
                    string exchange = (metal.Exchange ?? "").PadRight(w[2]);
                    string price = metal.Price.ToString("N2").PadLeft(w[3]);
                    string chpText = CliOutputHelper.FormatPercent(metal.Chp).PadLeft(w[4]);

                    Console.Write(CliConstants.DashboardTablePrefix + "│ " + id + " │ " + name + " │ " + exchange + " │ " + price + " │ ");
                    CliOutputHelper.WriteColoredValue(chpText, metal.Chp >= 0);
                    Console.WriteLine(" │");
                }
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
            }

            Console.WriteLine();
            CliOutputHelper.RenderViewFooter();
        });
    }
}
