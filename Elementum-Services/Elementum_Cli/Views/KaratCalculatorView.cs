using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;

namespace Elementum_Cli.Views;

public class KaratCalculatorView : MetalDetailViewBase
{
    protected override string ViewTitle => "KARAT & ALLOY";
    protected override string LoadingMessage => "Loading karat data…";

    private static string FormatEuro(decimal? value) => CliOutputHelper.FormatCurrency(value, "€");

    protected override async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            PriceHistory? ph = await HttpCall.GetPriceHistoryTodayWithLogicAsync(sym, name, ViewTitle);
            if (ph == null)
                return;

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
            string DiffStr(decimal? d) => d.HasValue ? "− " + CliOutputHelper.FormatCurrency(d.Value, "€") : "—";
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
}
