using Elementum.Application.Mapping;
using Elementum.Domain.Entities;

namespace Elementum.UnitTests.Application.Mapping;

public class PriceHistoryMapperTests
{
    // [R]IGHT-BICEP: Verifies that Metals entity correctly maps to MetalDto
    [Fact]
    public void ToDto_MapsMetalsCorrectly()
    {
        // Arrange
        var entity = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };

        // Act
        var dto = entity.ToDto();

        // Assert
        Assert.Equal(1, dto.Id);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.Name);
    }

    // [R]IGHT-BICEP: Verifies that PriceHistory entity maps to PriceHistoryDto with normalized properties
    [Fact]
    public void ToPriceHistoryDto_MapsPriceHistoryCorrectly()
    {
        // Arrange
        var metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };
        var entity = PriceHistory.Create(
            metalId: 1,
            currency: "USD",
            entryDate: new DateOnly(2026, 8, 17),
            price: 2500.50m,
            symbol: "XAU",
            chp: 1.2m,
            referenceTimestamp: 1723900000L,
            metal: metal);

        // Act
        var dto = entity.ToPriceHistoryDto();

        // Assert
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(1, dto.MetalId);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(1723900000L, dto.ReferenceTimestamp);
        Assert.Equal(2500.50m, dto.Price);
        Assert.Equal(1.2m, dto.Chp);
        Assert.NotNull(dto.Metal);
        Assert.Equal("Gold", dto.Metal.Name);
    }

    // [R]IGHT-BICEP: Verifies that DailyPriceSummary maps to DailyPriceSummaryDto including OHLC candles
    [Fact]
    public void ToDailyPriceSummaryDto_MapsSummaryCorrectly()
    {
        // Arrange
        var summary = DailyPriceSummary.Create(
            metalId: 1,
            currency: "USD",
            entryDate: new DateOnly(2026, 8, 17),
            openPrice: 2400m,
            highPrice: 2450m,
            lowPrice: 2390m,
            closePrice: 2420m,
            exchangeRateUsdEur: 1.15m);

        // Act
        var dto = summary.ToDailyPriceSummaryDto("XAU", "Gold");

        // Assert
        Assert.Equal(1, dto.MetalId);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.MetalName);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(2400m, dto.OpenPrice);
        Assert.Equal(2450m, dto.HighPrice);
        Assert.Equal(2390m, dto.LowPrice);
        Assert.Equal(2420m, dto.ClosePrice);
        Assert.Equal(1.15m, dto.ExchangeRateUsdEur);
    }

    // [R]IGHT-BICEP: Verifies that PriceHistory entity maps to TradingPriceDto with technical indicator metrics
    [Fact]
    public void ToTradingPriceDto_MapsPriceHistoryCorrectly()
    {
        // Arrange
        var metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };
        var entity = PriceHistory.Create(
            metalId: 1,
            currency: "USD",
            entryDate: new DateOnly(2026, 8, 17),
            price: 2500m,
            symbol: "XAUUSD",
            openPrice: 2480m,
            highPrice: 2510m,
            lowPrice: 2475m,
            prevClosePrice: 2470m,
            ch: 20m,
            chp: 0.81m,
            referenceTimestamp: 1723900000L,
            metal: metal);

        // Act
        var dto = entity.ToTradingPriceDto();

        // Assert
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.MetalName);
        Assert.Equal(1723900000L, dto.ReferenceTimestamp);
        Assert.Equal(2500m, dto.Price);
        Assert.Equal(20m, dto.Ch);
        Assert.Equal(0.81m, dto.Chp);
    }

    // [B]OUNDARY / [E]RROR: Verifies that null entity arguments throw ArgumentNullException
    [Fact]
    public void ToPriceHistoryDto_WhenEntityNull_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((PriceHistory)null!).ToPriceHistoryDto());
    }
}
