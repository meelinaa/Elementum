using Elementum.Application.DTOs;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;

namespace Elementum.Cli.Rendering;

/// <summary>
/// Dedicated renderer for the CLI Trading view: formats and prints technical indicators and OHLC tables.
/// Separates rendering logic and box-drawing from view lifecycle and input orchestration.
/// </summary>
public static class TradingRenderer
{
    public static void RenderTradingData(
        TradingPriceDto? item,
        string sym,
        string name,
        string currency,
        string currencySymbol,
        string viewTitle)
    {
        CliOutputHelper.SafeClear();
        CliOutputHelper.RenderViewHeader($"{viewTitle} — {name.ToUpperInvariant()} ({sym})");

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
        var rateStr = item.ExchangeRateUsdEur.HasValue ? item.ExchangeRateUsdEur.Value.ToString("0.0000") : "—";

        // Block 1: Trading data — High/Low, Open, Change
        CliOutputHelper.RenderSectionTitle("Trading data — High/Low, Open, Change");
        var topHeader = $"  EXCHANGE: {exchange}   CURRENCY: {currency}   DATE: {dateStr}";
        Console.WriteLine("  │" + topHeader.PadRight(66) + "│");
        Console.WriteLine("  ├─────────────────────────────────┬────────────────────────────────┤");

        var leftHigh = "  HIGH:".PadRight(16) + highStr.PadLeft(15) + "  ";
        var rightLow = "  LOW:".PadRight(16) + lowStr.PadLeft(14) + "  ";
        Console.WriteLine($"  │{leftHigh}│{rightLow}│");

        var leftOpen = "  OPEN:".PadRight(16) + openStr.PadLeft(15) + "  ";
        var rightRate = "  EXCH USD/EUR:".PadRight(18) + rateStr.PadLeft(12) + "  ";
        Console.WriteLine($"  │{leftOpen}│{rightRate}│");

        Console.Write("  │" + "  CH:".PadRight(16));
        CliOutputHelper.WriteColoredValue(chStr.PadLeft(15), (item.Ch ?? 0) >= 0);
        Console.Write("  │" + "  CHP:".PadRight(16));
        CliOutputHelper.WriteColoredValue(chpStr.PadLeft(14), (item.Chp ?? 0) >= 0);
        Console.WriteLine("  │");
        Console.WriteLine("  └─────────────────────────────────┴────────────────────────────────┘");

        // Block 2: Comparison — Today vs. Previous Close
        var currentPriceStr = CliOutputHelper.FormatCurrency(item.Price, currencySymbol);
        var prevCloseStr = CliOutputHelper.FormatCurrency(item.PrevClosePrice, currencySymbol);
        var diffStr = CliOutputHelper.FormatCurrencyWithSign(item.DifferencePrevClose, currencySymbol);
        var diffArrow = (item.DifferencePrevClose ?? 0) >= 0 ? "▲" : "▼";
        var diffCombined = $"{diffStr} {diffArrow}";
        var status = item.Status ?? ((item.DifferencePrevClose ?? 0) >= 0 ? "BULLISH ▲" : "BEARISH ▼");

        CliOutputHelper.RenderSectionTitle("Comparison — Today vs. Previous Close");
        Console.WriteLine("  │   CURRENT     │    OPEN       │    PREV CLOSE   │   DIFFERENCE   │");
        Console.WriteLine("  ├───────────────┼───────────────┼─────────────────┼────────────────┤");
        Console.Write($"  │ {currentPriceStr.PadLeft(13)} │ {openStr.PadLeft(13)} │ {prevCloseStr.PadLeft(15)} │ ");
        CliOutputHelper.WriteColoredValue(diffCombined.PadLeft(14), (item.DifferencePrevClose ?? 0) >= 0);
        Console.WriteLine(" │");
        Console.WriteLine("  └───────────────┴───────────────┴─────────────────┴────────────────┘");

        Console.Write("  Status: ");
        CliOutputHelper.WriteColoredValue(status, status.Contains("BULLISH"));
        Console.WriteLine();

        // Block 3: Volatility & Range
        var rangeStr = CliOutputHelper.FormatCurrency(item.VolatilityRange, currencySymbol);
        var rangePctStr = item.VolatilityPercent.HasValue ? item.VolatilityPercent.Value.ToString("0.##") + " %" : "—";

        CliOutputHelper.RenderSectionTitle("Daily volatility & range");
        var rowVol1 = "  DAY HIGH:".PadRight(16) + highStr.PadLeft(14) + "      " + "DAY LOW:".PadRight(16) + lowStr.PadLeft(14);
        var rowVol2 = "  RANGE:".PadRight(16) + rangeStr.PadLeft(14) + "      " + "PERCENT:".PadRight(16) + rangePctStr.PadLeft(14);
        Console.WriteLine("  │" + rowVol1.PadRight(66) + "│");
        Console.WriteLine("  │" + rowVol2.PadRight(66) + "│");
        Console.WriteLine("  └" + new string('─', 66) + "┘");

        DateTime? lastUpdate = item.ReferenceTimestamp.HasValue && item.ReferenceTimestamp.Value > 0
            ? DateTimeOffset.FromUnixTimeSeconds(item.ReferenceTimestamp.Value).ToLocalTime().DateTime
            : DateTime.Now;

        CliOutputHelper.RenderViewFooter(lastUpdate);
    }
}
