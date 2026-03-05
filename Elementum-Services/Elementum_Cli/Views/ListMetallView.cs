using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;

namespace Elementum_Cli.Views;

public class ListMetallView : IDetailView
{
    public void Render()
    {
        Console.Clear();
        CliOutputHelper.RenderViewHeader("METAL MASTER DATA");
        Console.WriteLine("  Loading metal list…");
        CliOutputHelper.RenderViewFooter();
        GetMetallList();
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }

    public static async Task GetMetallList()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetMetalListAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var metals = JsonSerializer.Deserialize<List<Metals>>(json, options);

            const int idWidth = 4;
            const int symbolWidth = 8;
            const int nameWidth = 20;

            Console.Clear();
            CliOutputHelper.RenderViewHeader("METAL MASTER DATA — GOLD, SILVER, PLATINUM");

            string topBorder = "  ┌──────┬──────────┬──────────────────────┐";
            string header = "  │ ID   │ SYMBOL   │ NAME                 │";
            string midBorder = "  ├──────┼──────────┼──────────────────────┤";
            string bottomBorder = "  └──────┴──────────┴──────────────────────┘";

            Console.WriteLine();
            Console.WriteLine(topBorder);
            Console.WriteLine(header);
            Console.WriteLine(midBorder);

            if (metals == null || metals.Count == 0)
            {
                Console.WriteLine("  │  No metals found.".PadRight(40) + "  │");
                Console.WriteLine(bottomBorder);
            }
            else
            {
                foreach (var metal in metals)
                {
                    string id = metal.Id.ToString().PadRight(idWidth);
                    string symbol = (metal.Symbol ?? "").PadRight(symbolWidth);
                    string name = (metal.Name ?? "").PadRight(nameWidth);
                    Console.WriteLine("  │ " + id + " │ " + symbol + " │ " + name + " │");
                }
                Console.WriteLine(bottomBorder);
            }

            CliOutputHelper.RenderViewFooter();
        });
    }
}
