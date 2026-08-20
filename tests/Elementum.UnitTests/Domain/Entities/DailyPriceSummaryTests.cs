using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;

namespace Elementum.UnitTests.Domain.Entities;

public class DailyPriceSummaryTests
{
    // [R]IGHT-BICEP: Verifies that valid arguments instantiate a DailyPriceSummary entity correctly
    [Fact]
    public void Create_ValidParameters_InstantiatesSuccessfully()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act
        var candle = DailyPriceSummary.Create(1, "usd", date, 4400m, 4450m, 4390m, 4420m, 1.15m);

        // Assert
        Assert.Equal(1, candle.MetalId);
        Assert.Equal("USD", candle.Currency); // Normalized
        Assert.Equal(date, candle.EntryDate);
        Assert.Equal(4400m, candle.OpenPrice);
        Assert.Equal(4450m, candle.HighPrice);
        Assert.Equal(4390m, candle.LowPrice);
        Assert.Equal(4420m, candle.ClosePrice);
        Assert.Equal(1.15m, candle.ExchangeRateUsdEur);
    }

    // [E]RROR: Verifies that invalid Metal ID throws InvalidMetalIdException
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidMetalId_ThrowsInvalidMetalIdException(int metalId)
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidMetalIdException>(() =>
            DailyPriceSummary.Create(metalId, "USD", new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    // [B]OUNDARY: Verifies that empty, whitespace, or null currency throws DomainValidationException
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidCurrency_ThrowsDomainValidationException(string? currency)
    {
        // Arrange & Act & Assert
        Assert.Throws<DomainValidationException>(() =>
            DailyPriceSummary.Create(1, currency!, new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    // [E]RROR: Verifies that non-supported ISO currency throws UnsupportedCurrencyException
    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void Create_UnsupportedCurrency_ThrowsUnsupportedCurrencyException(string currency)
    {
        // Arrange & Act & Assert
        Assert.Throws<UnsupportedCurrencyException>(() =>
            DailyPriceSummary.Create(1, currency, new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    // [E]RROR: Verifies that LowPrice greater than HighPrice throws PriceRangeInvalidException
    [Fact]
    public void Create_LowPriceGreaterThanHighPrice_ThrowsPriceRangeInvalidException()
    {
        // Arrange & Act & Assert
        Assert.Throws<PriceRangeInvalidException>(() =>
            DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 80m, 90m, 85m));
    }

    // [R]IGHT-BICEP: Verifies that applying higher and lower price ticks updates candle extremes
    [Fact]
    public void ApplyPriceTick_UpdatesHighAndLowPrices()
    {
        // Arrange
        var candle = DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 100m, 100m, 100m);

        // Act & Assert - higher tick
        candle.ApplyPriceTick(110m);
        Assert.Equal(110m, candle.HighPrice);
        Assert.Equal(100m, candle.LowPrice);

        // Act & Assert - lower tick
        candle.ApplyPriceTick(95m);
        Assert.Equal(110m, candle.HighPrice);
        Assert.Equal(95m, candle.LowPrice);

        // Act & Assert - close price update
        candle.ApplyPriceTick(105m, isClosePrice: true);
        Assert.Equal(105m, candle.ClosePrice);
    }

    // [I]NVERSE / INVARIANT: Verifies that price ticks inside current [Low, High] range leave extremes unchanged
    [Fact]
    public void ApplyPriceTick_PriceWithinExistingRange_LeavesHighAndLowUnchanged()
    {
        // Arrange
        var candle = DailyPriceSummary.Create(1, "EUR", new DateOnly(2026, 8, 19), 100m, 120m, 90m, 110m);

        // Act
        candle.ApplyPriceTick(105m); // Inside [90, 120]

        // Assert
        Assert.Equal(120m, candle.HighPrice);
        Assert.Equal(90m, candle.LowPrice);
    }

    // [B]OUNDARY: Verifies that price ticks exactly matching High or Low leave extremes unchanged
    [Fact]
    public void ApplyPriceTick_PriceMatchingExtremes_LeavesExtremesAccurate()
    {
        // Arrange
        var candle = DailyPriceSummary.Create(1, "EUR", new DateOnly(2026, 8, 19), 100m, 120m, 90m, 110m);

        // Act
        candle.ApplyPriceTick(120m);
        candle.ApplyPriceTick(90m);

        // Assert
        Assert.Equal(120m, candle.HighPrice);
        Assert.Equal(90m, candle.LowPrice);
    }

    // [R]IGHT-BICEP: UpdateCandle replaces OHLC and refreshes the concurrency token
    [Fact]
    public void UpdateCandle_ReplacesOhlcAndTouchesUpdatedAt()
    {
        // Arrange
        var candle = DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m);
        var previousUpdatedAt = candle.UpdatedAtUtc;

        // Act
        candle.UpdateCandle(200m, 220m, 190m, 210m);

        // Assert
        Assert.Equal(200m, candle.OpenPrice);
        Assert.Equal(220m, candle.HighPrice);
        Assert.Equal(190m, candle.LowPrice);
        Assert.Equal(210m, candle.ClosePrice);
        Assert.True(candle.UpdatedAtUtc >= previousUpdatedAt);
    }

    // [E]RROR: UpdateCandle rejects inverted high/low
    [Fact]
    public void UpdateCandle_WhenLowGreaterThanHigh_ThrowsPriceRangeInvalidException()
    {
        // Arrange
        var candle = DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m);

        // Act & Assert
        Assert.Throws<PriceRangeInvalidException>(() => candle.UpdateCandle(100m, 80m, 90m, 85m));
    }
}
