using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;

namespace Elementum_Cli.Views;

public class DashboardView : IDetailView
{
    public async void Render()
    {
        Console.Clear();
        CliOutputHelper.RenderViewHeader("CURRENT MARKET OVERVIEW");
        Console.WriteLine("  Loading market data…");
        CliOutputHelper.RenderViewFooter();
        await GetMetallList();
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }

    public static async Task GetMetallList()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetPriceHistoryAllTodayAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var priceHistories = JsonSerializer.Deserialize<List<PriceHistory>>(json, options);

            const int idWidth = 4;
            const int nameWidth = 10;
            const int exchWidth = 10;
            const int priceWidth = 10;
            const int changeWidth = 14;

            Console.Clear();
            CliOutputHelper.RenderViewHeader("CURRENT MARKET OVERVIEW");

            string topBorder = "   ┌──────┬────────────┬────────────┬────────────┬────────────────┐";
            string header = "   │ ID   │ NAME       │ EXCHANGE   │ PRICE USD  │ CHANGES (Chp)  │";
            string midBorder = "   ├──────┼────────────┼────────────┼────────────┼────────────────┤";
            string bottomBorder = "   └──────┴────────────┴────────────┴────────────┴────────────────┘";

            Console.WriteLine();
            Console.WriteLine(topBorder);
            Console.WriteLine(header);
            Console.WriteLine(midBorder);

            if (priceHistories == null || priceHistories.Count == 0)
            {
                Console.WriteLine("   │  No metals found.".PadRight(52) + "  │");
                Console.WriteLine(bottomBorder);
            }
            else
            {
                foreach (var metal in priceHistories)
                {
                    string id = (metal.Metal?.Symbol ?? metal.Symbol ?? "").PadRight(idWidth);
                    string name = (metal.Metal?.Name ?? "").PadRight(nameWidth);
                    string exchange = (metal.Exchange ?? "").PadRight(exchWidth);
                    string price = metal.Price.ToString("N2").PadLeft(priceWidth);
                    string chpText = $"{(metal.Chp < 0 ? "" : "+")}{metal.Chp:0.##}%".PadLeft(changeWidth);

                    Console.Write("   │ " + id + " │ " + name + " │ " + exchange + " │ " + price + " │ ");
                    Console.ForegroundColor = metal.Chp < 0 ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.Write(chpText);
                    Console.ResetColor();
                    Console.WriteLine(" │");
                }
                Console.WriteLine(bottomBorder);
            }

            Console.WriteLine();
            CliOutputHelper.RenderViewFooter();
        });
    }
}
