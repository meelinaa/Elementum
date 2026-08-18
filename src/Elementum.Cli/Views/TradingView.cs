using Elementum.Application.DTOs;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;

namespace Elementum.Cli.Views;

/// <summary>
/// Displays trading data for one metal: high/low, open, change, comparison vs previous close, volatility.
/// </summary>
public class TradingView : MetalDetailViewBase
{
    /// <inheritdoc />
    protected override string ViewTitle => "TRADING & DAILY ANALYSIS";

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading daily trading data…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var app = AppContext.Current!;
            var currency = app.SelectedCurrency;
            var currencySymbol = app.CurrencySymbol;

            Console.Clear();
            CliOutputHelper.RenderViewHeader($"{ViewTitle} — {name.ToUpperInvariant()} ({sym})");

            var item = await HttpCall.GetPriceHistoryTradingLatestAsync(sym, currency);

            if (item == null)
            {
                Console.WriteLine();
                Console.WriteLine("  " + CliOutputHelper.NoDataMessageForMetal);
                CliOutputHelper.RenderViewFooter();
                return;
            }

            var exchange = item.Exchange ?? "EDELMETALLE";
            var dateStr = item.EntryDate.ToString(CliConstants.DisplayDateFormat);
            var highStr = CliOutputHelper.FormatCurrency(item.HighPrice, currencySymbol);
            var lowStr = CliOutputHelper.FormatCurrency(item.LowPrice, currencySymbol);
            var openStr = CliOutputHelper.FormatCurrency(item.OpenPrice, currencySymbol);
            var chStr = CliOutputHelper.FormatCurrencyWithSign(item.Ch, currencySymbol);
            var chpStr = CliOutputHelper.FormatPercent(item.Chp);
            var rateStr = item.ExchangeRateUsdEur.HasValue ? item.ExchangeRateUsdEur.Value.ToString("0.0000") + " USD/EUR" : "—";

            // Block 1: Trading data
            CliOutputHelper.RenderSectionTitle("Trading data — High/Low, Open, Change");
            Console.WriteLine($"  │  EXCHANGE: {exchange,-11} CURRENCY: {currency,-4} DATE: {dateStr,-16} │");
            Console.WriteLine("  ├──────────────────────────────────────────────────────────────────┤");
            Console.WriteLine($"  │  HIGH:            {highStr,-12} │  LOW:            {lowStr,-10}     │");
            Console.WriteLine($"  │  OPEN:            {openStr,-12} │  EXCH RATE:      {rateStr,-10}     │");
            Console.Write("  │  CH:              ");
            CliOutputHelper.WriteColoredValue($"{chStr,-12}", (item.Ch ?? 0) >= 0);
            Console.Write(" │  CHP:            ");
            CliOutputHelper.WriteColoredValue($"{chpStr,-10}", (item.Chp ?? 0) >= 0);
            Console.WriteLine("     │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            // Block 2: Comparison Today vs. Previous Close
            var currentPriceStr = CliOutputHelper.FormatCurrency(item.Price, currencySymbol);
            var prevCloseStr = CliOutputHelper.FormatCurrency(item.PrevClosePrice, currencySymbol);
            var diffStr = CliOutputHelper.FormatCurrencyWithSign(item.DifferencePrevClose, currencySymbol);
            var diffArrow = (item.DifferencePrevClose ?? 0) >= 0 ? "▲" : "▼";
            var status = item.Status ?? ((item.DifferencePrevClose ?? 0) >= 0 ? "BULLISH ▲" : "BEARISH ▼");

            CliOutputHelper.RenderSectionTitle("Comparison — Today vs. Previous Close");
            Console.WriteLine("  │   CURRENT     │    OPEN       │    PREV CLOSE   │    DIFFERENCE  │");
            Console.WriteLine("  ├───────────────┼───────────────┼─────────────────┼────────────────┤");
            Console.Write($"  │ {currentPriceStr,-13} │  {openStr,-12} │  {prevCloseStr,-13}  │  ");
            CliOutputHelper.WriteColoredValue($"{diffStr,-12}{diffArrow}", (item.DifferencePrevClose ?? 0) >= 0);
            Console.Write(" │\n");

            Console.WriteLine("  └───────────────┴───────────────┴─────────────────┴────────────────┘");
            Console.Write("  Status: ");
            CliOutputHelper.WriteColoredValue(status, status.Contains("BULLISH"));
            Console.WriteLine();

            // Block 3: Volatility & Range
            var rangeStr = CliOutputHelper.FormatCurrency(item.VolatilityRange, currencySymbol);
            var rangePctStr = item.VolatilityPercent.HasValue ? item.VolatilityPercent.Value.ToString("0.##") + " %" : "—";

            CliOutputHelper.RenderSectionTitle("Daily volatility & range");
            Console.WriteLine($"  │  DAY HIGH:     {highStr,-12}   DAY LOW:      {lowStr,-20} │");
            Console.WriteLine($"  │  RANGE:        {rangeStr,-12}   PERCENT:      {rangePctStr,-20} │");
            Console.WriteLine("  └──────────────────────────────────────────────────────────────────┘");

            DateTime? lastUpdate = item.ReferenceTimestamp.HasValue && item.ReferenceTimestamp.Value > 0
                ? DateTimeOffset.FromUnixTimeSeconds(item.ReferenceTimestamp.Value).ToLocalTime().DateTime
                : DateTime.Now;

            CliOutputHelper.RenderViewFooter(lastUpdate);
        });
    }
}
