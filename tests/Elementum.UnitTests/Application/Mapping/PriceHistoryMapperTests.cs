using Elementum.Application.Mapping;
using Elementum.Domain.Entities;

namespace Elementum.UnitTests.Application.Mapping;

public class PriceHistoryMapperTests
{
    [Fact]
    public void ToDto_MapsMetalsCorrectly()
    {
        var entity = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };

        var dto = entity.ToDto();

        Assert.Equal(1, dto.Id);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.Name);
    }

    [Fact]
    public void ToPriceHistoryDto_MapsPriceHistoryCorrectly()
    {
        var entity = new PriceHistory
        {
            Id = 42,
            MetalId = 1,
            Currency = "USD",
            Exchange = "FOREX",
            Symbol = "XAU",
            EntryDate = new DateOnly(2026, 8, 17),
            Price = 2500.50m,
            Chp = 1.2m,
            Metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" }
        };

        var dto = entity.ToPriceHistoryDto();

        Assert.Equal(42, dto.Id);
        Assert.Equal(1, dto.MetalId);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(2500.50m, dto.Price);
        Assert.Equal(1.2m, dto.Chp);
        Assert.NotNull(dto.Metal);
        Assert.Equal("Gold", dto.Metal.Name);
    }

    [Fact]
    public void ToDailyPriceSummaryDto_MapsSummaryCorrectly()
    {
        var summary = DailyPriceSummary.Create(
            metalId: 1,
            currency: "USD",
            entryDate: new DateOnly(2026, 8, 17),
            openPrice: 2400m,
            highPrice: 2450m,
            lowPrice: 2390m,
            closePrice: 2420m,
            exchangeRateUsdEur: 1.15m);

        var dto = summary.ToDailyPriceSummaryDto("XAU", "Gold");

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

    [Fact]
    public void ToKaratPricesDto_MapsPriceHistoryCorrectly()
    {
        var entity = new PriceHistory
        {
            Id = 10,
            Symbol = "XAUUSD",
            Currency = "USD",
            EntryDate = new DateOnly(2026, 8, 17),
            PriceGram24k = 80m,
            PriceGram18k = 60m,
            Metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" }
        };

        var dto = entity.ToKaratPricesDto();

        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.MetalName);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(80m, dto.PriceGram24k);
        Assert.Equal(60m, dto.PriceGram18k);
    }

    [Fact]
    public void ToTradingPriceDto_MapsPriceHistoryCorrectly()
    {
        var entity = new PriceHistory
        {
            Id = 10,
            Symbol = "XAUUSD",
            Currency = "USD",
            Exchange = "FOREX",
            EntryDate = new DateOnly(2026, 8, 17),
            ReferenceTimestamp = "1723900000",
            OpenTime = "1723890000",
            Price = 2500m,
            OpenPrice = 2480m,
            PrevClosePrice = 2470m,
            HighPrice = 2510m,
            LowPrice = 2475m,
            Ch = 20m,
            Chp = 0.81m,
            Ask = 2501m,
            Bid = 2499m,
            Metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" }
        };

        var dto = entity.ToTradingPriceDto();

        Assert.Equal(10, dto.Id);
        Assert.Equal("XAU", dto.Symbol);
        Assert.Equal("Gold", dto.MetalName);
        Assert.Equal(1723900000L, dto.ReferenceTimestamp);
        Assert.Equal(1723890000L, dto.OpenTime);
        Assert.Equal(2500m, dto.Price);
        Assert.Equal(20m, dto.Ch);
        Assert.Equal(0.81m, dto.Chp);
        Assert.Equal(2501m, dto.Ask);
        Assert.Equal(2499m, dto.Bid);
    }
}
