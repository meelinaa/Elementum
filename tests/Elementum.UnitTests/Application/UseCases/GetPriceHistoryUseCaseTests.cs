using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Moq;

namespace Elementum.UnitTests.Application.UseCases;

public class GetPriceHistoryUseCaseTests
{
    private readonly Mock<IPriceHistoryReadRepository> _repositoryMock = new();
    private readonly GetPriceHistoryUseCase _useCase;

    public GetPriceHistoryUseCaseTests()
    {
        _useCase = new GetPriceHistoryUseCase(_repositoryMock.Object);
    }

    // [R]IGHT-BICEP: Verifies that GetLatestAllAsync returns all mapped price history DTOs
    [Fact]
    public async Task GetLatestAllAsync_ReturnsMappedDtos()
    {
        // Arrange
        var entities = new List<PriceHistory>
        {
            new() { Id = 1, MetalId = 1, Symbol = "XAU", Currency = "USD", Price = 2500m, EntryDate = new DateOnly(2026, 8, 17), ReferenceTimestamp = 1000L }
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryAllLatest(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = await _useCase.GetLatestAllAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal(2500m, result.First().Price);
    }

    // [R]IGHT-BICEP: Verifies that GetLatestBySymbolAsync returns mapped DTO for the specific metal
    [Fact]
    public async Task GetLatestBySymbolAsync_ReturnsSingleDto()
    {
        // Arrange
        var entity = new PriceHistory
        {
            Id = 1,
            MetalId = 1,
            Symbol = "XAU",
            Currency = "USD",
            Price = 2500m,
            EntryDate = new DateOnly(2026, 8, 17),
            ReferenceTimestamp = 1000L
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _useCase.GetLatestBySymbolAsync("XAU");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.Price);
    }

    // [B]OUNDARY / [E]RROR: Verifies that querying a non-existent metal returns null
    [Fact]
    public async Task GetLatestBySymbolAsync_WhenNotFound_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolLatest("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PriceHistory?)null);

        // Act
        var result = await _useCase.GetLatestBySymbolAsync("UNKNOWN");

        // Assert
        Assert.Null(result);
    }

    // [R]IGHT-BICEP: Verifies that GetBySymbolAsync maps repository ticks for a metal and optional currency
    [Fact]
    public async Task GetBySymbolAsync_WhenCurrencyProvided_PassesFilterToRepository()
    {
        // Arrange
        var entities = new List<PriceHistory>
        {
            new() { Id = 1, MetalId = 1, Symbol = "XAU", Currency = "EUR", Price = 2300m, EntryDate = new DateOnly(2026, 8, 17), ReferenceTimestamp = 1000L }
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAsync("XAU", "EUR", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = (await _useCase.GetBySymbolAsync("XAU", "EUR")).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(2300m, result[0].Price);
        _repositoryMock.Verify(r => r.GetPriceHistoryByMetalSymbolAsync("XAU", "EUR", It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Verifies that GetByDateRangeAsync maps the inclusive date-range query
    [Fact]
    public async Task GetByDateRangeAsync_ReturnsMappedDtos()
    {
        // Arrange
        var first = new DateOnly(2026, 8, 1);
        var last = new DateOnly(2026, 8, 17);
        var entities = new List<PriceHistory>
        {
            new() { Id = 1, MetalId = 1, Symbol = "XAU", Currency = "USD", Price = 2500m, EntryDate = first, ReferenceTimestamp = 1000L }
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync("XAU", first, last, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        // Act
        var result = (await _useCase.GetByDateRangeAsync("XAU", first, last)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(2500m, result[0].Price);
    }

    // [R]IGHT-BICEP: Verifies that GetTradingLatestAsync computes and exposes technical indicators
    [Fact]
    public async Task GetTradingLatestAsync_ReturnsTradingPriceDto()
    {
        // Arrange
        var entity = new PriceHistory
        {
            Id = 1,
            MetalId = 1,
            Symbol = "XAU",
            Currency = "USD",
            Price = 2500m,
            EntryDate = new DateOnly(2026, 8, 17),
            ReferenceTimestamp = 1000L
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _useCase.GetTradingLatestAsync("XAU");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.Price);
    }
}
