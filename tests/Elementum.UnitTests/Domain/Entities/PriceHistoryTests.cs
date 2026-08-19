using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;

namespace Elementum.UnitTests.Domain.Entities;

public class PriceHistoryTests
{
    [Fact]
    public void Create_WithValidParameters_InstantiatesSuccessfully()
    {
        var date = new DateOnly(2026, 8, 17);
        var priceHistory = PriceHistory.Create(
            metalId: 1,
            currency: "usd",
            entryDate: date,
            price: 2500.50m,
            symbol: "XAU",
            highPrice: 2520m,
            lowPrice: 2490m);

        Assert.Equal(1, priceHistory.MetalId);
        Assert.Equal("USD", priceHistory.Currency); // Normalized
        Assert.Equal(date, priceHistory.EntryDate);
        Assert.Equal(2500.50m, priceHistory.Price);
        Assert.Equal(2520m, priceHistory.HighPrice);
        Assert.Equal(2490m, priceHistory.LowPrice);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMetalId_ThrowsInvalidMetalIdException(int invalidMetalId)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<InvalidMetalIdException>(() =>
            PriceHistory.Create(invalidMetalId, "USD", date, 2000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithNonPositivePrice_ThrowsInvalidPriceException(decimal invalidPrice)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<InvalidPriceException>(() =>
            PriceHistory.Create(1, "USD", date, invalidPrice));
    }

    [Fact]
    public void Create_WhenLowPriceGreaterThanHighPrice_ThrowsPriceRangeInvalidException()
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<PriceRangeInvalidException>(() =>
            PriceHistory.Create(1, "USD", date, 2000m, lowPrice: 2100m, highPrice: 2000m));
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("XYZ")]
    public void Create_WithUnsupportedCurrency_ThrowsUnsupportedCurrencyException(string unsupportedCurrency)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<UnsupportedCurrencyException>(() =>
            PriceHistory.Create(1, unsupportedCurrency, date, 2000m));
    }
}
