using Elementum.Shared.DTOs;
using Elementum_Cli.Api;
using Elementum_Cli.Constants;
using Elementum_Cli.Output;
using Elementum_Cli.Views.Interfaces;
using System.Text.Json;
using System.Linq;

namespace Elementum_Cli.Views;

/// <summary>
/// Displays the current market overview: latest price per metal in a table (ID, Name, Exchange, Price USD, Chp).
/// Data from GET history/all/latest.
/// </summary>
public class DashboardView : AsyncDetailViewBase
{
    /// <inheritdoc />
    protected override string ViewTitle => CliStrings.DashboardTitle;

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading market data…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetPriceHistoryAllTodayAsync();
            var items = JsonSerializer.Deserialize<List<PriceHistoryDto>>(json, HttpCall.DefaultJsonOptions);

            var w = CliConstants.DashboardColumnWidths;

            Console.Clear();
            CliOutputHelper.RenderViewHeader(ViewTitle);

            Console.WriteLine();
            Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.DashboardTablePrefix, w));
            Console.WriteLine(TableFormatter.BuildRow(CliConstants.DashboardTablePrefix, w, new[] { "ID", "NAME", "EXCHANGE", "PRICE USD", "CHANGES (Chp)" }));
            Console.WriteLine(TableFormatter.BuildMidBorder(CliConstants.DashboardTablePrefix, w));

            if (items == null || items.Count == 0)
            {
                Console.WriteLine(TableFormatter.BuildEmptyRow(CliConstants.DashboardTablePrefix, w, CliOutputHelper.NoMetalsFoundMessage));
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
            }
            else
            {
                foreach (var item in items)
                {
                    string id = (item.Metal?.Symbol ?? item.Symbol ?? "").PadRight(w[0]);
                    string name = (item.Metal?.Name ?? "").PadRight(w[1]);
                    string exchange = (item.Exchange ?? "").PadRight(w[2]);
                    string price = item.Price.ToString("N2").PadLeft(w[3]);
                    string chpText = CliOutputHelper.FormatPercent(item.Chp).PadLeft(w[4]);

                    Console.Write(CliConstants.DashboardTablePrefix + "│ " + id + " │ " + name + " │ " + exchange + " │ " + price + " │ ");
                    CliOutputHelper.WriteColoredValue(chpText, (item.Chp ?? 0) >= 0);
                    Console.WriteLine(" │");
                }
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.DashboardTablePrefix, w));
            }

            Console.WriteLine();
            DateOnly? lastUpdate = items is { Count: > 0 } ? items.Max(i => i.EntryDate) : null;
            CliOutputHelper.RenderViewFooter(lastUpdate);
        });
    }
}
