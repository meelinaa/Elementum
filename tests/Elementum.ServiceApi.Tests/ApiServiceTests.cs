using Elementum.Infrastructure.Data;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.Objects;
using Elementum_ServiceApi.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Elementum.ServiceApi.Tests;

public class ApiServiceTests
{
    [Fact]
    public async Task GetAllMetals_DelegatesToDbContext_AndReturnsDtos()
    {
        var options = new DbContextOptionsBuilder<ElementumDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new ElementumDbContext(options);
        dbContext.Metals.Add(new Metals { Id = 1, Symbol = "XAU", Name = "Gold" });
        await dbContext.SaveChangesAsync();

        var sut = new ApiService(dbContext);

        var result = await sut.GetAllMetals(CancellationToken.None);

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("XAU", list[0].Symbol);
        Assert.Equal("Gold", list[0].Name);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsNull_WhenNoResult()
    {
        var dbMock = new Mock<IElementumDbContext>();
        dbMock.Setup(db => db.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
              .ReturnsAsync((PriceHistory?)null);

        var sut = new ApiService(dbMock.Object);

        var result = await sut.GetPriceHistoryByMetalSymbolLatest("XAU", CancellationToken.None);

        Assert.Null(result);
        dbMock.Verify(db => db.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()), Times.Once);
    }
}
