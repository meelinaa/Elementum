using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;
using Elementum.Domain.ValueObjects;

namespace Elementum.UnitTests.Domain.ValueObjects;

public class OhlcCandleTests
{
    // [R]IGHT-BICEP: Valid constructor parameters construct immutable OhlcCandle
    [Fact]
    public void Constructor_WhenValidValues_ConstructsOhlcCandle()
    {
        // Arrange & Act
        var candle = new OhlcCandle(open: 2450m, high: 2500m, low: 2400m, close: 2480m);

        // Assert
        Assert.Equal(2450m, candle.Open);
        Assert.Equal(2500m, candle.High);
        Assert.Equal(2400m, candle.Low);
        Assert.Equal(2480m, candle.Close);
        Assert.Equal(100m, candle.VolatilityRange);
        Assert.Equal(4.17m, candle.VolatilityPercent);
    }

    // RIGHT-[B]ICEP: Negative price parameters violate domain invariants
    [Theory]
    [InlineData(-1, 2500, 2400, 2480)]
    [InlineData(2450, -1, 2400, 2480)]
    [InlineData(2450, 2500, -1, 2480)]
    [InlineData(2450, 2500, 2400, -1)]
    public void Constructor_WhenNegativePrice_ThrowsInvalidPriceException(
        decimal open, decimal high, decimal low, decimal close)
    {
        // Act & Assert
        Assert.Throws<InvalidPriceException>(() => new OhlcCandle(open, high, low, close));
    }

    // RIGHT-BIC[E]P: High price strictly less than Low price violates domain invariants
    [Fact]
    public void Constructor_WhenHighLessThanLow_ThrowsPriceRangeInvalidException()
    {
        // Act & Assert
        Assert.Throws<PriceRangeInvalidException>(() => new OhlcCandle(open: 2450m, high: 2399m, low: 2400m, close: 2420m));
    }

    // [R]IGHT-BICEP: FromSinglePrice initializes all 4 OHLC fields to the single price
    [Fact]
    public void FromSinglePrice_InitializesAllFieldsToSinglePrice()
    {
        // Arrange & Act
        var candle = OhlcCandle.FromSinglePrice(2500m);

        // Assert
        Assert.Equal(2500m, candle.Open);
        Assert.Equal(2500m, candle.High);
        Assert.Equal(2500m, candle.Low);
        Assert.Equal(2500m, candle.Close);
        Assert.Equal(0m, candle.VolatilityRange);
    }

    // RIGHT-B[I]CEP: ApplyTick expands High and Low and updates Close on close tick
    [Fact]
    public void ApplyTick_ExpandsHighAndLowCorrectly()
    {
        // Arrange
        var initial = OhlcCandle.FromSinglePrice(2500m);

        // Act
        var afterHigher = initial.ApplyTick(2550m);
        var afterLower = afterHigher.ApplyTick(2450m);
        var finalClose = afterLower.ApplyTick(2480m, isCloseTick: true);

        // Assert
        Assert.Equal(2500m, finalClose.Open);
        Assert.Equal(2550m, finalClose.High);
        Assert.Equal(2450m, finalClose.Low);
        Assert.Equal(2480m, finalClose.Close);
        Assert.Equal(100m, finalClose.VolatilityRange);
    }

    // [R]IGHT-BICEP: DailyPriceSummary.ToOhlcCandle converts entity properties into typed value object
    [Fact]
    public void DailyPriceSummary_ToOhlcCandle_MapsAllValues()
    {
        // Arrange
        var summary = DailyPriceSummary.Create(
            metalId: 1,
            currency: "EUR",
            entryDate: new DateOnly(2026, 8, 22),
            openPrice: 2200m,
            highPrice: 2250m,
            lowPrice: 2180m,
            closePrice: 2240m);

        // Act
        var candle = summary.ToOhlcCandle();

        // Assert
        Assert.Equal(2200m, candle.Open);
        Assert.Equal(2250m, candle.High);
        Assert.Equal(2180m, candle.Low);
        Assert.Equal(2240m, candle.Close);
    }
}
