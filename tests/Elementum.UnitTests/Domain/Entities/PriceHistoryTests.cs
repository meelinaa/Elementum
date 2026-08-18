using Elementum.Domain.Entities;

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
            exchange: "FOREXCOM",
            highPrice: 2520m,
            lowPrice: 2490m,
            ask: 2501m,
            bid: 2500m,
            priceGram24k: 80.39m);

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
    public void Create_WithInvalidMetalId_ThrowsArgumentOutOfRangeException(int invalidMetalId)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceHistory.Create(invalidMetalId, "USD", date, 2000m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithNonPositivePrice_ThrowsArgumentOutOfRangeException(decimal invalidPrice)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PriceHistory.Create(1, "USD", date, invalidPrice));
    }

    [Fact]
    public void Create_WhenLowPriceGreaterThanHighPrice_ThrowsArgumentException()
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<ArgumentException>(() =>
            PriceHistory.Create(1, "USD", date, 2000m, lowPrice: 2100m, highPrice: 2000m));
    }

    [Fact]
    public void Create_WhenBidGreaterThanAsk_ThrowsArgumentException()
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<ArgumentException>(() =>
            PriceHistory.Create(1, "USD", date, 2000m, ask: 1990m, bid: 2010m));
    }

    [Fact]
    public void CalculatePurityGramPrice_ValidRatio_ComputesAccuratePrice()
    {
        var priceHistory = PriceHistory.Create(
            metalId: 1,
            currency: "USD",
            entryDate: new DateOnly(2026, 8, 17),
            price: 2488.278m,
            priceGram24k: 80.00m);

        // 18k = 75.0% purity -> 80.00 * 0.75 = 60.00
        var price18k = priceHistory.CalculatePurityGramPrice(0.750m);
        Assert.Equal(60.00m, price18k);

        // 14k = 58.5% purity -> 80.00 * 0.585 = 46.80
        var price14k = priceHistory.CalculatePurityGramPrice(0.585m);
        Assert.Equal(46.80m, price14k);
    }

    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("XYZ")]
    public void Create_WithUnsupportedCurrency_ThrowsArgumentException(string unsupportedCurrency)
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Throws<ArgumentException>(() =>
            PriceHistory.Create(1, unsupportedCurrency, date, 2000m));
    }
}
