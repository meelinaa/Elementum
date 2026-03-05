using System.Linq;
using Elementum.Shared.Objects;
using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Views;

public class TradingView : IDetailView
{
    public void Render()
    {
        Console.Clear();
        if (!currentSelectedMetal.HasValue)
        {
            CliOutputHelper.RenderMetalSelectionPrompt();
            return;
        }

        var sym = MetallHelper.GetSymbol(currentSelectedMetal.Value);
        var name = MetallHelper.GetName(currentSelectedMetal.Value);

        CliOutputHelper.RenderViewHeader($"TRADING & DAILY ANALYSIS — {name.ToUpperInvariant()} ({sym})");
        Console.WriteLine("  Loading daily data…");
        CliOutputHelper.RenderViewFooter();

        _ = LoadAndRenderAsync(sym, name);
    }

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
            CliOutputHelper.RenderViewHeader($"TRADING & DAILY ANALYSIS — {name.ToUpperInvariant()} ({sym})");

            if (ph == null)
            {
                Console.WriteLine();
                Console.WriteLine("  No daily data available for this metal.");
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var exchange = ph.Exchange ?? "—";
            var currency = ph.Currency ?? "USD";
            var dateStr = ph.EntryDate.ToString("yyyy-MM-dd");
            var bidStr = ph.Bid.HasValue ? ph.Bid.Value.ToString("N2") + " $" : "—";
            var askStr = ph.Ask.HasValue ? ph.Ask.Value.ToString("N2") + " $" : "—";
            var spread = (ph.Ask.HasValue && ph.Bid.HasValue) ? (ph.Ask.Value - ph.Bid.Value).ToString("N2") + " $" : "—";
            var highStr = ph.HighPrice.HasValue ? ph.HighPrice.Value.ToString("N2") + " $" : "—";
            var lowStr = ph.LowPrice.HasValue ? ph.LowPrice.Value.ToString("N2") + " $" : "—";
            var openStr = ph.OpenPrice.HasValue ? ph.OpenPrice.Value.ToString("N2") + " $" : "—";
            var chStr = ph.Ch.HasValue ? ph.Ch.Value.ToString("N2") + " $" : "—";
            var chpStr = ph.Chp.HasValue ? (ph.Chp.Value >= 0 ? "+" : "") + ph.Chp.Value.ToString("0.##") + " %" : "—";

            // Block 1: Trading data
            CliOutputHelper.RenderSectionTitle("Trading data — Bid/Ask, Spread, High/Low");
            Console.WriteLine($"  │  EXCHANGE: {exchange,-8} CURRENCY: {currency,-4} DATE: {dateStr}              │");
            Console.WriteLine("  ├──────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"  │  BID (Sell):      {bidStr,-12} │  ASK (Buy):      {askStr,-10}     │");
            Console.WriteLine($"  │  HIGH:            {highStr,-12} │  LOW:            {lowStr,-10}     │");
            Console.WriteLine($"  │  OPEN:            {openStr,-12} │  SPREAD:         {spread,-10}     │");
            Console.Write($"  │  CH/CHP:          ");
            Console.ForegroundColor = (chpStr.StartsWith("-") ? ConsoleColor.Red : ConsoleColor.Green);
            Console.Write($"{ chStr,-6}");
            Console.ResetColor();
            Console.Write(" / ");
            Console.ForegroundColor = ( chpStr.StartsWith("-") ? ConsoleColor.Red : ConsoleColor.Green);
            Console.Write($"{chpStr,-35}");
            Console.ResetColor();
            Console.Write("  │\n");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 2: Comparison Today vs. Previous Close
            var priceStr = ph.Price.ToString("N2") + " $";
            var prevCloseStr = ph.PrevClosePrice.HasValue ? ph.PrevClosePrice.Value.ToString("N2") + " $" : "—";
            var openPriceStr = ph.OpenPrice.HasValue ? ph.OpenPrice.Value.ToString("N2") + " $" : "—";
            decimal? diff = ph.PrevClosePrice.HasValue ? ph.Price - ph.PrevClosePrice.Value : null;
            var diffStr = diff.HasValue ? (diff.Value >= 0 ? "+" : "") + diff.Value.ToString("N2") + " $" : "—";
            var diffArrow = (ph.Chp ?? 0) >= 0 ? "▲" : "▼";
            var status = (ph.Chp ?? 0) >= 0 ? "BULLISH ▲" : "BEARISH ▼";

            CliOutputHelper.RenderSectionTitle("Comparison — Today vs. Previous Close");
            Console.WriteLine("  │   CURRENT     │    OPEN       │    PREV CLOSE   │    DIFFERENCE  │");
            Console.WriteLine("  ├───────────────┼───────────────┼─────────────────┼────────────────┤");
            Console.Write($"  │ {priceStr,-13} │  {openPriceStr,-12} │  {prevCloseStr,-13}  │  ");
            Console.ForegroundColor = (diffStr.StartsWith("-") ? ConsoleColor.Red : ConsoleColor.Green);
            Console.Write($"{diffStr,-12}{diffArrow}");
            Console.ResetColor();
            Console.Write(" │\n");
            
            Console.WriteLine("  └───────────────┴───────────────┴─────────────────┴────────────────┘");
            Console.WriteLine($"  Status: {status}");

            // Block 3: Volatility
            var prevClose = ph.PrevClosePrice ?? ph.Price;
            var range = (ph.HighPrice.HasValue && ph.LowPrice.HasValue)
                ? (ph.HighPrice.Value - ph.LowPrice.Value)
                : (decimal?)null;
            var rangeStr = range.HasValue ? range.Value.ToString("N2") + " $" : "—";
            var rangePct = (prevClose > 0 && range.HasValue)
                ? (range.Value / prevClose * 100)
                : (decimal?)null;
            var rangePctStr = rangePct.HasValue ? rangePct.Value.ToString("0.##") + " %" : "—";

            CliOutputHelper.RenderSectionTitle("Daily volatility & range");
            Console.WriteLine($"  │  DAY HIGH:     {highStr,-12}   DAY LOW:      {lowStr,-20} │");
            Console.WriteLine($"  │  RANGE:        {rangeStr,-12}   PERCENT:      {rangePctStr,-20} │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 4: Data integrity & timestamps (formerly TimestampView)
            var openTimeStr = ph.OpenTime.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(ph.OpenTime.Value).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC" : "—";
            var refTimeStr = ph.ReferenceTimestamp.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(ph.ReferenceTimestamp.Value).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC" : "—";
            CliOutputHelper.RenderSectionTitle("Data integrity & timestamps");
            Console.WriteLine($"  │  Metal ID:      {ph.Id,-45}    │");
            Console.WriteLine($"  │  EntryDate:     {ph.EntryDate,-45}    │");
            Console.WriteLine($"  │  Open time:     {openTimeStr,-45}    │");         
            Console.WriteLine($"  │  Ref time:      {refTimeStr,-45}    │");        
            Console.WriteLine($"  │  Source:        {ph.Symbol ?? sym,-45}    │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

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
