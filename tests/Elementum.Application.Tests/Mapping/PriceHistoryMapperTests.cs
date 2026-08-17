using Elementum.Application.Mapping;
using Elementum.Domain.Entities;

namespace Elementum.Application.Tests.Mapping;

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
            Chp = 1.2m
        };

        var dto = entity.ToPriceHistoryDto();

        Assert.Equal(42, dto.Id);
        Assert.Equal(1, dto.MetalId);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(2500.50m, dto.Price);
        Assert.Equal(1.2m, dto.Chp);
    }
}
