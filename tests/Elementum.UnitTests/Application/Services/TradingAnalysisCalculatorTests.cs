using Elementum.Application.Services;

namespace Elementum.UnitTests.Application.Services;

public class TradingAnalysisCalculatorTests
{
    [Fact]
    public void Calculate_BullishScenario_ComputesExpectedMetrics()
    {
        // current = 2500, open = 2400, high = 2550, low = 2380, prevClose = 2450
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(2500m, 2400m, 2550m, 2380m, 2450m);

        Assert.Equal(100m, ch);
        Assert.Equal(4.17m, chp); // (100 / 2400) * 100 = 4.1666... -> 4.17
        Assert.Equal(50m, diffPrevClose);
        Assert.Equal("BULLISH ▲", status);
        Assert.Equal(170m, volatilityRange); // 2550 - 2380 = 170
        Assert.Equal(7.14m, volatilityPercent); // (170 / 2380) * 100 = 7.1428... -> 7.14
    }

    [Fact]
    public void Calculate_BearishScenario_ComputesExpectedMetrics()
    {
        // current = 2300, open = 2400, high = 2420, low = 2280, prevClose = 2350
        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPercent) =
            TradingAnalysisCalculator.Calculate(2300m, 2400m, 2420m, 2280m, 2350m);

        Assert.Equal(-100m, ch);
        Assert.Equal(-4.17m, chp);
        Assert.Equal(-50m, diffPrevClose);
        Assert.Equal("BEARISH ▼", status);
        Assert.Equal(140m, volatilityRange);
        Assert.Equal(6.14m, volatilityPercent);
    }
}
