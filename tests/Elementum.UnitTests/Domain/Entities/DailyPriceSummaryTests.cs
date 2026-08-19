using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;

namespace Elementum.UnitTests.Domain.Entities;

public class DailyPriceSummaryTests
{
    [Fact]
    public void Create_ValidParameters_InstantiatesSuccessfully()
    {
        var date = new DateOnly(2026, 8, 17);
        var candle = DailyPriceSummary.Create(1, "usd", date, 4400m, 4450m, 4390m, 4420m, 1.15m);

        Assert.Equal(1, candle.MetalId);
        Assert.Equal("USD", candle.Currency); // Normalized
        Assert.Equal(date, candle.EntryDate);
        Assert.Equal(4400m, candle.OpenPrice);
        Assert.Equal(4450m, candle.HighPrice);
        Assert.Equal(4390m, candle.LowPrice);
        Assert.Equal(4420m, candle.ClosePrice);
        Assert.Equal(1.15m, candle.ExchangeRateUsdEur);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidMetalId_ThrowsInvalidMetalIdException(int metalId)
    {
        Assert.Throws<InvalidMetalIdException>(() =>
            DailyPriceSummary.Create(metalId, "USD", new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_InvalidCurrency_ThrowsDomainValidationException(string? currency)
    {
        Assert.Throws<DomainValidationException>(() =>
            DailyPriceSummary.Create(1, currency!, new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    public void Create_UnsupportedCurrency_ThrowsUnsupportedCurrencyException(string currency)
    {
        Assert.Throws<UnsupportedCurrencyException>(() =>
            DailyPriceSummary.Create(1, currency, new DateOnly(2026, 8, 17), 100m, 120m, 90m, 110m));
    }

    [Fact]
    public void Create_LowPriceGreaterThanHighPrice_ThrowsPriceRangeInvalidException()
    {
        Assert.Throws<PriceRangeInvalidException>(() =>
            DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 80m, 90m, 85m));
    }

    [Fact]
    public void ApplyPriceTick_UpdatesHighAndLowPrices()
    {
        var candle = DailyPriceSummary.Create(1, "USD", new DateOnly(2026, 8, 17), 100m, 100m, 100m, 100m);

        candle.ApplyPriceTick(110m);
        Assert.Equal(110m, candle.HighPrice);
        Assert.Equal(100m, candle.LowPrice);

        candle.ApplyPriceTick(95m);
        Assert.Equal(110m, candle.HighPrice);
        Assert.Equal(95m, candle.LowPrice);

        candle.ApplyPriceTick(105m, isClosePrice: true);
        Assert.Equal(105m, candle.ClosePrice);
    }
}
