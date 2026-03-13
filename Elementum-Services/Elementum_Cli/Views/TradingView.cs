using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;

namespace Elementum_Cli.Views;

public class TradingView : MetalDetailViewBase
{
    protected override string ViewTitle => "TRADING & DAILY ANALYSIS";
    protected override string LoadingMessage => "Loading daily data…";

    protected override async Task LoadAndRenderAsync(string sym, string name) // todo: consider caching this data for the session to allow faster re-rendering when user goes back and forth between metals
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            PriceHistory? ph = await HttpCall.GetPriceHistoryTodayWithLogicAsync(sym, name, ViewTitle);
            if (ph == null)
                return;

            var exchange = ph.Exchange ?? "—";
            var currency = ph.Currency ?? "USD";
            var dateStr = ph.EntryDate.ToString("yyyy-MM-dd");
            var bidStr = CliOutputHelper.FormatCurrency(ph.Bid, "$");
            var askStr = CliOutputHelper.FormatCurrency(ph.Ask, "$");
            var spread = (ph.Ask.HasValue && ph.Bid.HasValue) ? CliOutputHelper.FormatCurrency(ph.Ask.Value - ph.Bid.Value, "$") : "—";
            var highStr = CliOutputHelper.FormatCurrency(ph.HighPrice, "$");
            var lowStr = CliOutputHelper.FormatCurrency(ph.LowPrice, "$");
            var openStr = CliOutputHelper.FormatCurrency(ph.OpenPrice, "$");
            var chStr = CliOutputHelper.FormatCurrency(ph.Ch, "$");
            var chpStr = CliOutputHelper.FormatPercent(ph.Chp);

            // Block 1: Trading data
            CliOutputHelper.RenderSectionTitle("Trading data — Bid/Ask, Spread, High/Low");
            Console.WriteLine($"  │  EXCHANGE: {exchange,-8} CURRENCY: {currency,-4} DATE: {dateStr}              │");
            Console.WriteLine("  ├──────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"  │  BID (Sell):      {bidStr,-12} │  ASK (Buy):      {askStr,-10}     │");
            Console.WriteLine($"  │  HIGH:            {highStr,-12} │  LOW:            {lowStr,-10}     │");
            Console.WriteLine($"  │  OPEN:            {openStr,-12} │  SPREAD:         {spread,-10}     │");
            Console.Write($"  │  CH/CHP:          ");
            CliOutputHelper.WriteColoredValue($"{chStr,-6}", (ph.Chp ?? 0) >= 0);
            Console.Write(" / ");
            CliOutputHelper.WriteColoredValue($"{chpStr,-35}", (ph.Chp ?? 0) >= 0);
            Console.Write("  │\n");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 2: Comparison Today vs. Previous Close
            var priceStr = CliOutputHelper.FormatCurrency(ph.Price, "$");
            var prevCloseStr = CliOutputHelper.FormatCurrency(ph.PrevClosePrice, "$");
            var openPriceStr = CliOutputHelper.FormatCurrency(ph.OpenPrice, "$");
            decimal? diff = ph.PrevClosePrice.HasValue ? ph.Price - ph.PrevClosePrice.Value : null;
            var diffStr = CliOutputHelper.FormatCurrencyWithSign(diff, "$");
            var diffArrow = (ph.Chp ?? 0) >= 0 ? "▲" : "▼";
            var status = (ph.Chp ?? 0) >= 0 ? "BULLISH ▲" : "BEARISH ▼";

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
            var prevClose = ph.PrevClosePrice ?? ph.Price;
            var range = (ph.HighPrice.HasValue && ph.LowPrice.HasValue)
                ? (ph.HighPrice.Value - ph.LowPrice.Value)
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
}
