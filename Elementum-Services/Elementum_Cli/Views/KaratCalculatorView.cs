using Elementum_Cli.Api;
using Elementum_Cli.Output;

namespace Elementum_Cli.Views;

/// <summary>
/// Displays price per gram by purity (24k–10k) and alloy discount vs 24k for one metal.
/// Data from GET history/{symbol}/latest/karat.
/// </summary>
public class KaratCalculatorView : MetalDetailViewBase
{
    /// <inheritdoc />
    protected override string ViewTitle => "KARAT & ALLOY";

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading karat data…";

    /// <summary>Formats a decimal as currency with € symbol, or "—" if null.</summary>
    private static string FormatEuro(decimal? value) => CliOutputHelper.FormatCurrency(value, "€");

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            Console.Clear();
            CliOutputHelper.RenderViewHeader($"{ViewTitle} — {name.ToUpperInvariant()} ({sym})");

            var dto = await HttpCall.GetPriceHistoryKaratLatestAsync(sym);
            if (dto == null)
            {
                Console.WriteLine();
                Console.WriteLine("  " + CliOutputHelper.NoDataMessageForMetal);
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var p24 = dto.PriceGram24k;
            var p22 = dto.PriceGram22k;
            var p21 = dto.PriceGram21k;
            var p18 = dto.PriceGram18k;
            var p16 = dto.PriceGram16k;
            var p14 = dto.PriceGram14k;
            var p10 = dto.PriceGram10k;

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

            CliOutputHelper.RenderViewFooter(dto.EntryDate);
        });
    }
}
