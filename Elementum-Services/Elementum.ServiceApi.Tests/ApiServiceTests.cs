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
    public async Task GetMetalBySymbol_DelegatesToDbContext_AndReturnsDto()
    {
        var metal = new Metals { Id = 1, Symbol = "XAG", Name = "Silver" };
        _dbMock.Setup(db => db.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()))
               .ReturnsAsync(metal);

        var result = await _sut.GetMetalBySymbol("XAG", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("XAG", result.Symbol);
        Assert.Equal("Silver", result.Name);
        _dbMock.Verify(db => db.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPriceHistoryByMetalSymbolLatest_Throws_WhenNoResult()
    {
        // Arrange
        _dbMock.Setup(db => db.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
               .ReturnsAsync((PriceHistory?)null);

        // Act / Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.GetPriceHistoryByMetalSymbolLatest("XAU", CancellationToken.None));

        Assert.Contains("XAU", ex.Message);
    }
}
