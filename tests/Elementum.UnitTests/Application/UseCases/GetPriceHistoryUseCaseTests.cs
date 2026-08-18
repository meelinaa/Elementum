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

    [Fact]
    public async Task GetLatestAllAsync_ReturnsMappedDtos()
    {
        var entities = new List<PriceHistory>
        {
            new() { Id = 1, MetalId = 1, Symbol = "XAU", Currency = "USD", Price = 2500m, EntryDate = new DateOnly(2026, 8, 17), ReferenceTimestamp = 1000L }
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryAllLatest(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var result = await _useCase.GetLatestAllAsync();

        Assert.Single(result);
        Assert.Equal(2500m, result.First().Price);
    }

    [Fact]
    public async Task GetLatestBySymbolAsync_ReturnsSingleDto()
    {
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

        var result = await _useCase.GetLatestBySymbolAsync("XAU");

        Assert.NotNull(result);
        Assert.Equal(2500m, result.Price);
    }

    [Fact]
    public async Task GetTradingLatestAsync_ReturnsTradingPriceDto()
    {
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

        var result = await _useCase.GetTradingLatestAsync("XAU");

        Assert.NotNull(result);
        Assert.Equal(2500m, result.Price);
    }
}
