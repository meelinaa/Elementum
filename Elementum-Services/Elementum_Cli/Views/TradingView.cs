using Elementum.Shared.DTOs;
using Elementum_Cli.Api;
using Elementum_Cli.Output;
using System.Text.Json;

namespace Elementum_Cli.Views;

public class TradingView : MetalDetailViewBase
{
    protected override string ViewTitle => "TRADING & DAILY ANALYSIS";
    protected override string LoadingMessage => "Loading daily data…";

    protected override async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            Console.Clear();
            CliOutputHelper.RenderViewHeader($"{ViewTitle} — {name.ToUpperInvariant()} ({sym})");

            var json = await HttpCall.GetPriceHistoryTradingLatestAsync(sym);
            var items = JsonSerializer.Deserialize<TradingPriceDto>(json, HttpCall.DefaultJsonOptions);

            if (items == null)
            {
                Console.WriteLine();
                Console.WriteLine("  " + CliOutputHelper.NoDataMessageForMetal);
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var exchange = items.Exchange ?? "—";
            var currency = items.Currency ?? "USD";
            var dateStr = items.EntryDate.ToString("yyyy-MM-dd");
            var bidStr = CliOutputHelper.FormatCurrency(items.Bid, "$");
            var askStr = CliOutputHelper.FormatCurrency(items.Ask, "$");
            var spread = (items.Ask.HasValue && items.Bid.HasValue) ? CliOutputHelper.FormatCurrency(items.Ask.Value - items.Bid.Value, "$") : "—";
            var highStr = CliOutputHelper.FormatCurrency(items.HighPrice, "$");
            var lowStr = CliOutputHelper.FormatCurrency(items.LowPrice, "$");
            var openStr = CliOutputHelper.FormatCurrency(items.OpenPrice, "$");
            var chStr = CliOutputHelper.FormatCurrency(items.Ch, "$");
            var chpStr = CliOutputHelper.FormatPercent(items.Chp);

            // Block 1: Trading data
            CliOutputHelper.RenderSectionTitle("Trading data — Bid/Ask, Spread, High/Low");
            Console.WriteLine($"  │  EXCHANGE: {exchange,-8} CURRENCY: {currency,-4} DATE: {dateStr}              │");
            Console.WriteLine("  ├──────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"  │  BID (Sell):      {bidStr,-12} │  ASK (Buy):      {askStr,-10}     │");
            Console.WriteLine($"  │  HIGH:            {highStr,-12} │  LOW:            {lowStr,-10}     │");
            Console.WriteLine($"  │  OPEN:            {openStr,-12} │  SPREAD:         {spread,-10}     │");
            Console.Write($"  │  CH/CHP:          ");
            CliOutputHelper.WriteColoredValue($"{chStr,-6}", (items.Chp ?? 0) >= 0);
            Console.Write(" / ");
            CliOutputHelper.WriteColoredValue($"{chpStr,-35}", (items.Chp ?? 0) >= 0);
            Console.Write("  │\n");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 2: Comparison Today vs. Previous Close
            var priceStr = CliOutputHelper.FormatCurrency(items.Price, "$");
            var prevCloseStr = CliOutputHelper.FormatCurrency(items.PrevClosePrice, "$");
            var openPriceStr = CliOutputHelper.FormatCurrency(items.OpenPrice, "$");
            decimal? diff = items.PrevClosePrice.HasValue ? items.Price - items.PrevClosePrice.Value : null;
            var diffStr = CliOutputHelper.FormatCurrencyWithSign(diff, "$");
            var diffArrow = (items.Chp ?? 0) >= 0 ? "▲" : "▼";
            var status = (items.Chp ?? 0) >= 0 ? "BULLISH ▲" : "BEARISH ▼";

            CliOutputHelper.RenderSectionTitle("Comparison — Today vs. Previous Close");
            Console.WriteLine("  │   CURRENT     │    OPEN       │    PREV CLOSE   │    DIFFERENCE  │");
            Console.WriteLine("  ├───────────────┼───────────────┼─────────────────┼────────────────┤");
            Console.Write($"  │ {priceStr,-13} │  {openPriceStr,-12} │  {prevCloseStr,-13}  │  ");
            CliOutputHelper.WriteColoredValue($"{diffStr,-12}{diffArrow}", !diffStr.StartsWith("-"));
            Console.Write(" │\n");

            Console.WriteLine("  └───────────────┴───────────────┴─────────────────┴────────────────┘");
            Console.Write("  Status: ");
            CliOutputHelper.WriteColoredValue(status, status.Contains("BULLISH"));
            Console.WriteLine();

            // Block 3: Volatility
            var prevClose = items.PrevClosePrice ?? items.Price;
            var range = (items.HighPrice.HasValue && items.LowPrice.HasValue)
                ? (items.HighPrice.Value - items.LowPrice.Value)
                : (decimal?)null;
            var rangeStr = CliOutputHelper.FormatCurrency(range, "$");
            var rangePct = (prevClose > 0 && range.HasValue)
                ? (range.Value / prevClose * 100)
                : (decimal?)null;
            var rangePctStr = rangePct.HasValue ? rangePct.Value.ToString("0.##") + " %" : "—";

            CliOutputHelper.RenderSectionTitle("Daily volatility & range");
            Console.WriteLine($"  │  DAY HIGH:     {highStr,-12}   DAY LOW:      {lowStr,-20} │");
            Console.WriteLine($"  │  RANGE:        {rangeStr,-12}   PERCENT:      {rangePctStr,-20} │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 4: Data integrity & timestamps (API sends Unix timestamps in seconds, not milliseconds)
            var openTimeStr = items.OpenTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(items.OpenTime.Value).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC" : "—";
            var refTimeStr = items.ReferenceTimestamp.HasValue ? DateTimeOffset.FromUnixTimeSeconds(items.ReferenceTimestamp.Value).UtcDateTime.ToString("yyyy-MM-dd HH:mm") + " UTC" : "—";
            var sourceStr = !string.IsNullOrEmpty(items.Exchange) ? $"{items.Exchange}:{items.Symbol}{items.Currency}" : (items.Symbol ?? sym);
            CliOutputHelper.RenderSectionTitle("Data integrity & timestamps");
            Console.WriteLine($"  │  Metal ID:      {items.Id,-45}    │");
            Console.WriteLine($"  │  EntryDate:     {items.EntryDate:d}                                      │");
            Console.WriteLine($"  │  Open time:     {openTimeStr,-45}    │");
            Console.WriteLine($"  │  Ref time:      {refTimeStr,-45}    │");
            Console.WriteLine($"  │  Source:        {sourceStr,-45}    │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            CliOutputHelper.RenderViewFooter();
        });
    }
}
