using System.Linq;
using Elementum.Shared.Objects;
using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Views;

public class KaratCalculatorView : IDetailView
{
    public void Render()
    {
        Console.Clear();
        if (!currentSelectedMetal.HasValue)
        {
            CliOutputHelper.RenderMetalSelectionPrompt();
            return;
        }

        var name = MetallHelper.GetName(currentSelectedMetal.Value);
        var sym = MetallHelper.GetSymbol(currentSelectedMetal.Value);

        CliOutputHelper.RenderViewHeader($"KARAT & ALLOY — {name.ToUpperInvariant()} ({sym})");
        Console.WriteLine("  Loading karat data…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderAsync(sym, name);
    }

    private static string FormatEuro(decimal? value) => value.HasValue ? value.Value.ToString("N2") + " €" : "—";

    private static async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetPriceHistoryTodayAsync(sym);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            PriceHistory? ph = null;
            try
            {
                var list = JsonSerializer.Deserialize<List<PriceHistory>>(json, options);
                ph = list?.FirstOrDefault();
            }
            catch
            {
                ph = JsonSerializer.Deserialize<PriceHistory>(json, options);
            }

            Console.Clear();
            CliOutputHelper.RenderViewHeader($"KARAT & ALLOY — {name.ToUpperInvariant()} ({sym})");

            if (ph == null)
            {
                Console.WriteLine();
                Console.WriteLine("  No daily data available for this metal.");
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var p24 = ph.PriceGram24k;
            var p22 = ph.PriceGram22k;
            var p21 = ph.PriceGram21k;
            var p18 = ph.PriceGram18k;
            var p16 = ph.PriceGram16k;
            var p14 = ph.PriceGram14k;
            var p10 = ph.PriceGram10k;

            // Block 1: Price per gram (24k–10k)
            CliOutputHelper.RenderSectionTitle("Price per gram (EUR) — 24k to 10k");
            Console.WriteLine("  │  Purity       │  Price/gram     │  Purity       │  Price/gram    │");
            Console.WriteLine("  ├───────────────┼─────────────────┼───────────────┼────────────────┤");
            Console.WriteLine($"  │  24k (Fine)   │  {FormatEuro(p24),-14} │  16k          │  {FormatEuro(p16),-13} │");
            Console.WriteLine($"  │  22k          │  {FormatEuro(p22),-14} │  14k          │  {FormatEuro(p14),-13} │");
            Console.WriteLine($"  │  21k          │  {FormatEuro(p21),-14} │  10k          │  {FormatEuro(p10),-13} │");
            Console.WriteLine($"  │  18k          │  {FormatEuro(p18),-14} │               │                │");
            Console.WriteLine("  └───────────────┴─────────────────┴───────────────┴────────────────┘");

            // Block 2: Alloy analysis (discount vs. 24k)
            decimal? Diff(decimal? gramPrice) => (p24.HasValue && gramPrice.HasValue) ? p24.Value - gramPrice.Value : null;
            string DiffStr(decimal? d) => d.HasValue ? "− " + d.Value.ToString("N2") + " €" : "—";
            var d18 = Diff(p18);
            var d14 = Diff(p14);
            var d10 = Diff(p10);

            CliOutputHelper.RenderSectionTitle("Alloy analysis — value discount vs. 24k");
            Console.WriteLine("  │  Purity     │  Price/gram    │  Difference vs. 24k               │");
            Console.WriteLine("  ├─────────────┼────────────────┼───────────────────────────────────┤");
            Console.WriteLine($"  │  24k        │  {FormatEuro(p24),-13} │                                   │");
            Console.WriteLine($"  │  18k        │  {FormatEuro(p18),-13} │  {DiffStr(d18),-32} │");
            Console.WriteLine($"  │  14k        │  {FormatEuro(p14),-13} │  {DiffStr(d14),-32} │");
            Console.WriteLine($"  │  10k        │  {FormatEuro(p10),-13} │  {DiffStr(d10),-32} │");
            Console.WriteLine("  └─────────────┴────────────────┴───────────────────────────────────┘");

            Console.WriteLine();
            Console.WriteLine($"  Last update (EntryDate): {ph.EntryDate:yyyy-MM-dd}");

            CliOutputHelper.RenderViewFooter();
        });
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        if (!currentSelectedMetal.HasValue)
        {
            var metall = MetallHelper.FromKey(key.KeyChar) ?? MetallHelper.FromConsoleKey(key.Key);
            if (metall.HasValue)
            {
                currentSelectedMetal = metall;
                Render();
            }
            else if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                currentState = AppState.Menu;
                currentDetailView = null;
                CliOutputHelper.RenderMenu();
            }
            return;
        }
        if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
        {
            currentState = AppState.Menu;
            currentDetailView = null;
            currentSelectedMetal = null;
            CliOutputHelper.RenderMenu();
        }
    }
}
