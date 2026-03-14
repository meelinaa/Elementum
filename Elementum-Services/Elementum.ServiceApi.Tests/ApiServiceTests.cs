using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;
using Elementum_ServiceApi.Services;
using Moq;

namespace Elementum.ServiceApi.Tests;

public class ApiServiceTests
{
    private readonly Mock<IElementumDbContext> _dbMock = new();
    private readonly ApiService _sut;

    public ApiServiceTests()
    {
        _sut = new ApiService(_dbMock.Object);
    }

    [Fact]
    public async Task GetAllMetals_DelegatesToDbContext_AndReturnsDtos()
    {
        var metals = new List<Metals> { new() { Id = 1, Symbol = "XAU", Name = "Gold" } };
        _dbMock.Setup(db => db.GetMetalsAll(It.IsAny<CancellationToken>()))
               .ReturnsAsync(metals);

        var result = await _sut.GetAllMetals(CancellationToken.None);

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("XAU", list[0].Symbol);
        Assert.Equal("Gold", list[0].Name);
        _dbMock.Verify(db => db.GetMetalsAll(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_ReturnsNull_WhenNoResult()
    {
        _dbMock.Setup(db => db.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
               .ReturnsAsync((PriceHistory?)null);

        var result = await _sut.GetPriceHistoryByMetalSymbolLatest("XAU", CancellationToken.None);

        Assert.Null(result);
        _dbMock.Verify(db => db.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()), Times.Once);
    }

}
