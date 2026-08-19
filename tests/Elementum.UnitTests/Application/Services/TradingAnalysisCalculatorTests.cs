using Elementum.Application.Services;
using Elementum.Domain.Constants;

namespace Elementum.UnitTests.Application.Services;

public class TradingAnalysisCalculatorTests
{
    // [R]IGHT-BICEP: Verifies that bullish price movements yield positive change and Bullish status
    [Fact]
    public void Calculate_BullishScenario_ComputesExpectedMetrics()
    {
        // Arrange
        // current = 2500, open = 2400, high = 2550, low = 2380, prevClose = 2450
        decimal currentPrice = 2500m;
        decimal openPrice = 2400m;
        decimal highPrice = 2550m;
        decimal lowPrice = 2380m;
        decimal prevClose = 2450m;

        // Act
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

        // Assert
        Assert.Equal(100m, ch);
        Assert.Equal(4.17m, chp); // (100 / 2400) * 100 = 4.1666... -> 4.17
        Assert.Equal(50m, diffPrevClose);
        Assert.Equal(DomainConstants.Trading.BullishStatus, status);
        Assert.Equal(170m, volatilityRange); // 2550 - 2380 = 170
        Assert.Equal(7.14m, volatilityPercent); // (170 / 2380) * 100 = 7.1428... -> 7.14
    }

    // [R]IGHT-BICEP: Verifies that bearish price movements yield negative change and Bearish status
    [Fact]
    public void Calculate_BearishScenario_ComputesExpectedMetrics()
    {
        // Arrange
        // current = 2300, open = 2400, high = 2420, low = 2280, prevClose = 2350
        decimal currentPrice = 2300m;
        decimal openPrice = 2400m;
        decimal highPrice = 2420m;
        decimal lowPrice = 2280m;
        decimal prevClose = 2350m;

        // Act
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

        // Assert
        Assert.Equal(-100m, ch);
        Assert.Equal(-4.17m, chp);
        Assert.Equal(-50m, diffPrevClose);
        Assert.Equal(DomainConstants.Trading.BearishStatus, status);
        Assert.Equal(140m, volatilityRange);
        Assert.Equal(6.14m, volatilityPercent);
    }

    // [B]OUNDARY: Verifies behavior when all prices are identical (zero movement, zero volatility)
    [Fact]
    public void Calculate_ZeroMovementAndZeroVolatility_ReturnsZeroMetricsAndBullishStatus()
    {
        // Arrange
        decimal flatPrice = 2000m;

        // Act
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(flatPrice, flatPrice, flatPrice, flatPrice, flatPrice);

        // Assert
        Assert.Equal(0m, ch);
        Assert.Equal(0m, chp);
        Assert.Equal(0m, diffPrevClose);
        Assert.Equal(DomainConstants.Trading.BullishStatus, status);
        Assert.Equal(0m, volatilityRange);
        Assert.Equal(0m, volatilityPercent);
    }

    // [B]OUNDARY / [E]RROR: Verifies that zero open price or zero low price avoids division by zero
    [Fact]
    public void Calculate_ZeroOpenAndLowPrice_DoesNotThrowAndReturnsZeroPercentages()
    {
        // Arrange
        decimal currentPrice = 100m;
        decimal openPrice = 0m;
        decimal highPrice = 120m;
        decimal lowPrice = 0m;
        decimal prevClose = 90m;

        // Act
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

        // Assert
        Assert.Equal(100m, ch);
        Assert.Equal(0m, chp); // Handled safely without DivideByZeroException
        Assert.Equal(10m, diffPrevClose);
        Assert.Equal(120m, volatilityRange);
        Assert.Equal(0m, volatilityPercent); // Handled safely without DivideByZeroException
    }

    // [C]ROSS-CHECK: Verifies calculation against an independent reference formula
    [Theory]
    [InlineData(2500, 2400, 2600, 2300, 2450)]
    [InlineData(1800, 1850, 1900, 1750, 1820)]
    [InlineData(3200, 3100, 3300, 3050, 3150)]
    public void Calculate_AgainstIndependentReferenceFormula_MatchesExpectedResults(
        decimal currentPrice, decimal openPrice, decimal highPrice, decimal lowPrice, decimal prevClose)
    {
        // Arrange - independent reference formula calculation
        decimal expectedCh = currentPrice - openPrice;
        decimal expectedChp = Math.Round((expectedCh / openPrice) * 100m, 2, MidpointRounding.AwayFromZero);
        decimal expectedDiffPrevClose = currentPrice - prevClose;
        string expectedStatus = expectedDiffPrevClose >= 0 ? DomainConstants.Trading.BullishStatus : DomainConstants.Trading.BearishStatus;
        decimal expectedVolatilityRange = highPrice - lowPrice;
        decimal expectedVolatilityPct = Math.Round((expectedVolatilityRange / lowPrice) * 100m, 2, MidpointRounding.AwayFromZero);

        // Act
        var result = TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

        // Assert
        Assert.Equal(expectedCh, result.Ch);
        Assert.Equal(expectedChp, result.Chp);
        Assert.Equal(expectedDiffPrevClose, result.DiffPrevClose);
        Assert.Equal(expectedStatus, result.Status);
        Assert.Equal(expectedVolatilityRange, result.VolatilityRange);
        Assert.Equal(expectedVolatilityPct, result.VolatilityPercent);
    }
}
