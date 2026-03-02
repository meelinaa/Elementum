using Elementum.Infrastructure.Data.Interfaces;
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
    public async Task GetAllMetals_DelegatesToDbContext()
    {
        // Arrange
        var metals = new List<Metals> { new() { Id = 1, Symbol = "XAU", Name = "Gold" } };
        _dbMock.Setup(db => db.GetMetalsAll(It.IsAny<CancellationToken>()))
               .ReturnsAsync(metals);

        // Act
        var result = await _sut.GetAllMetals(CancellationToken.None);

        // Assert
        Assert.Same(metals, result);
        _dbMock.Verify(db => db.GetMetalsAll(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetMetalBySymbol_DelegatesToDbContext()
    {
        // Arrange
        var metal = new Metals { Id = 1, Symbol = "XAG", Name = "Silver" };
        _dbMock.Setup(db => db.GetMetalBySymbol("XAG", It.IsAny<CancellationToken>()))
               .ReturnsAsync(metal);

        // Act
        var result = await _sut.GetMetalBySymbol("XAG", CancellationToken.None);

        // Assert
        Assert.Same(metal, result);
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
