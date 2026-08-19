using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;

namespace Elementum.UnitTests.Domain.Entities;

public class PriceHistoryTests
{
    // [R]IGHT-BICEP: Verifies that valid parameters successfully create and normalize a PriceHistory entity
    [Fact]
    public void Create_WithValidParameters_InstantiatesSuccessfully()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act
        var priceHistory = PriceHistory.Create(
            metalId: 1,
            currency: "usd",
            entryDate: date,
            price: 2500.50m,
            symbol: "XAU",
            highPrice: 2520m,
            lowPrice: 2490m);

        // Assert
        Assert.Equal(1, priceHistory.MetalId);
        Assert.Equal("USD", priceHistory.Currency); // Normalized
        Assert.Equal(date, priceHistory.EntryDate);
        Assert.Equal(2500.50m, priceHistory.Price);
        Assert.Equal(2520m, priceHistory.HighPrice);
        Assert.Equal(2490m, priceHistory.LowPrice);
    }

    // [B]OUNDARY: Verifies that flat prices where High == Low == Price are accepted at boundary
    [Fact]
    public void Create_WhenHighLowAndPriceAreEqual_InstantiatesSuccessfully()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 19);
        decimal flatPrice = 3000m;

        // Act
        var priceHistory = PriceHistory.Create(
            metalId: 1,
            currency: "EUR",
            entryDate: date,
            price: flatPrice,
            highPrice: flatPrice,
            lowPrice: flatPrice);

        // Assert
        Assert.Equal(flatPrice, priceHistory.Price);
        Assert.Equal(flatPrice, priceHistory.HighPrice);
        Assert.Equal(flatPrice, priceHistory.LowPrice);
    }

    // [E]RROR: Verifies that non-positive Metal ID throws InvalidMetalIdException
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidMetalId_ThrowsInvalidMetalIdException(int invalidMetalId)
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act & Assert
        Assert.Throws<InvalidMetalIdException>(() =>
            PriceHistory.Create(invalidMetalId, "USD", date, 2000m));
    }

    // [E]RROR: Verifies that non-positive Price throws InvalidPriceException
    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_WithNonPositivePrice_ThrowsInvalidPriceException(decimal invalidPrice)
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act & Assert
        Assert.Throws<InvalidPriceException>(() =>
            PriceHistory.Create(1, "USD", date, invalidPrice));
    }

    // [E]RROR: Verifies that LowPrice greater than HighPrice throws PriceRangeInvalidException
    [Fact]
    public void Create_WhenLowPriceGreaterThanHighPrice_ThrowsPriceRangeInvalidException()
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act & Assert
        Assert.Throws<PriceRangeInvalidException>(() =>
            PriceHistory.Create(1, "USD", date, 2000m, lowPrice: 2100m, highPrice: 2000m));
    }

    // [E]RROR: Verifies that unsupported currency strings throw UnsupportedCurrencyException
    [Theory]
    [InlineData("CHF")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("XYZ")]
    public void Create_WithUnsupportedCurrency_ThrowsUnsupportedCurrencyException(string unsupportedCurrency)
    {
        // Arrange
        var date = new DateOnly(2026, 8, 17);

        // Act & Assert
        Assert.Throws<UnsupportedCurrencyException>(() =>
            PriceHistory.Create(1, unsupportedCurrency, date, 2000m));
    }
}
