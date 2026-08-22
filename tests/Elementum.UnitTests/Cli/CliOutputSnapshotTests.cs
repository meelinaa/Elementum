using Elementum.Application.DTOs;
using Elementum.Cli.Output;
using Elementum.Cli.Rendering;

namespace Elementum.Cli.Tests;

public class CliOutputSnapshotTests
{
    // [R]IGHT-BICEP: DashboardRenderer snapshot with populated items produces well-formed table
    [Fact]
    public void DashboardRenderer_WithItems_RendersExpectedSnapshot()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var overview = new LiveMarketOverviewDto
            {
                Items =
                [
                    new LiveMetalPriceDto
                    {
                        Symbol = "XAU",
                        Name = "Gold",
                        PriceUsd = 2500.50m,
                        PriceEur = 2280.30m,
                        ChpEur = 0.45m
                    },
                    new LiveMetalPriceDto
                    {
                        Symbol = "XAG",
                        Name = "Silver",
                        PriceUsd = 30.20m,
                        PriceEur = 27.50m,
                        ChpEur = -0.15m
                    }
                ],
                ExchangeRateUsdEur = 1.1575m,
                Timestamp = 1723900000
            };

            // Act
            DashboardRenderer.RenderDashboard(overview, "MARKET OVERVIEW — LIVE PRICES");

            var output = sw.ToString();

            // Assert
            Assert.Contains("MARKET OVERVIEW — LIVE PRICES", output);
            Assert.Contains("ID", output);
            Assert.Contains("NAME", output);
            Assert.Contains("PRICE USD", output);
            Assert.Contains("PRICE EUR", output);
            Assert.Contains("CHANGES (Chp)", output);
            Assert.Contains("XAU", output);
            Assert.Contains("Gold", output);
            Assert.Contains(overview.Items[0].PriceUsd.ToString("N2"), output);
            Assert.Contains(overview.Items[0].PriceEur.ToString("N2"), output);
            Assert.Contains("XAG", output);
            Assert.Contains("Silver", output);
            Assert.DoesNotContain("Exchange rate USD/EUR", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // RIGHT-BIC[E]P: DashboardRenderer snapshot with empty items renders empty state row
    [Fact]
    public void DashboardRenderer_WhenEmpty_RendersEmptyStateRow()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var overview = new LiveMarketOverviewDto { Items = [] };

            // Act
            DashboardRenderer.RenderDashboard(overview, "MARKET OVERVIEW — LIVE PRICES");

            var output = sw.ToString();

            // Assert
            Assert.Contains("MARKET OVERVIEW — LIVE PRICES", output);
            Assert.Contains(CliOutputHelper.NoMetalsFoundMessage, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // [R]IGHT-BICEP: TradingRenderer snapshot produces all 3 structured sections
    [Fact]
    public void TradingRenderer_WithItem_RendersAllThreeSections()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            var item = new TradingPriceDto
            {
                Symbol = "XAU",
                MetalName = "Gold",
                Exchange = "EDELMETALLE",
                Currency = "EUR",
                EntryDate = new DateOnly(2026, 8, 22),
                Price = 2280.30m,
                OpenPrice = 2270.00m,
                HighPrice = 2290.00m,
                LowPrice = 2260.00m,
                PrevClosePrice = 2265.00m,
                Ch = 10.30m,
                Chp = 0.45m,
                DifferencePrevClose = 15.30m,
                VolatilityRange = 30.00m,
                VolatilityPercent = 1.33m,
                Status = "BULLISH ▲",
                ExchangeRateUsdEur = 1.1575m,
                ReferenceTimestamp = 1723900000
            };

            // Act
            TradingRenderer.RenderTradingData(item, "XAU", "Gold", "EUR", "€", "TRADING & DAILY ANALYSIS");

            var output = sw.ToString();

            // Assert Section 1
            Assert.Contains("Trading data — High/Low, Open, Change", output);
            Assert.Contains("HIGH:", output);
            Assert.Contains("LOW:", output);
            Assert.Contains("OPEN:", output);
            Assert.Contains("CH:", output);
            Assert.Contains("CHP:", output);
            Assert.Contains(item.HighPrice!.Value.ToString("N2"), output);
            Assert.Contains(item.LowPrice!.Value.ToString("N2"), output);

            // Assert Section 2
            Assert.Contains("Comparison — Today vs. Previous Close", output);
            Assert.Contains("CURRENT", output);
            Assert.Contains("PREV CLOSE", output);
            Assert.Contains("DIFFERENCE", output);
            Assert.Contains("Status:", output);
            Assert.Contains("BULLISH ▲", output);

            // Assert Section 3
            Assert.Contains("Daily volatility & range", output);
            Assert.Contains("DAY HIGH:", output);
            Assert.Contains("DAY LOW:", output);
            Assert.Contains("RANGE:", output);
            Assert.Contains("PERCENT:", output);
            Assert.Contains(item.VolatilityRange!.Value.ToString("N2"), output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // RIGHT-BIC[E]P: TradingRenderer snapshot when item is null renders no data message
    [Fact]
    public void TradingRenderer_WhenNullItem_RendersNoDataMessage()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            // Act
            TradingRenderer.RenderTradingData(null, "XAU", "Gold", "EUR", "€", "TRADING & DAILY ANALYSIS");

            var output = sw.ToString();

            // Assert
            Assert.Contains("TRADING & DAILY ANALYSIS — GOLD (XAU)", output);
            Assert.Contains(CliOutputHelper.NoDataMessageForMetal, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    // [R]IGHT-BICEP: InfoRenderer renders expected About Elementum view header and text
    [Fact]
    public void InfoRenderer_RendersExpectedSnapshot()
    {
        // Arrange
        var originalOut = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            // Act
            InfoRenderer.RenderInfo();

            var output = sw.ToString();

            // Assert
            Assert.Contains("INFO — SYSTEM ARCHITECTURE & FEATURES", output);
            Assert.Contains("About Elementum", output);
            Assert.Contains("Gold (XAU)", output);
            Assert.Contains("Clean Architecture", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
