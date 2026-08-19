using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.UnitTests.Application.UseCases;

public class GetDailyCandlesUseCaseTests
{
    private readonly Mock<IPriceHistoryReadRepository> _repositoryMock = new();
    private readonly Mock<ILogger<GetDailyCandlesUseCase>> _loggerMock = new();
    private readonly GetDailyCandlesUseCase _useCase;

    public GetDailyCandlesUseCaseTests()
    {
        _useCase = new GetDailyCandlesUseCase(_repositoryMock.Object, _loggerMock.Object);
    }

    // [B]OUNDARY / [E]RROR: Verifies that empty, null, or whitespace symbols fail validation and return Failure result
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ExecuteAsync_WhenSymbolIsInvalid_ReturnsFailure(string? symbol)
    {
        // Arrange & Act
        var result = await _useCase.ExecuteAsync(symbol!);

        // Assert
        Assert.True(result.IsFailure);
    }

    // [E]RROR: Verifies that querying an unknown metal symbol returns a Domain Error Failure result
    [Fact]
    public async Task ExecuteAsync_WhenMetalNotFound_ReturnsFailure()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetMetalBySymbol("XYZ", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metals?)null);

        // Act
        var result = await _useCase.ExecuteAsync("XYZ");

        // Assert
        Assert.True(result.IsFailure);
    }

    // [R]IGHT-BICEP: Verifies that querying valid metal returns mapped DailyPriceSummaryDto list with all OHLC values
    [Fact]
    public async Task ExecuteAsync_WhenMetalFound_ReturnsMappedDtos()
    {
        // Arrange
        var metal = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };
        var date = new DateOnly(2026, 8, 17);
        var candle = DailyPriceSummary.Create(1, "USD", date, 4400m, 4450m, 4390m, 4420m, 1.15m);

        _repositoryMock.Setup(r => r.GetMetalBySymbol("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metal);

        _repositoryMock.Setup(r => r.GetDailySummariesAsync("XAU", "USD", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary> { candle });

        // Act
        var result = await _useCase.ExecuteAsync("XAU", "USD");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("XAU", result.Value[0].Symbol);
        Assert.Equal("Gold", result.Value[0].MetalName);
        Assert.Equal(4400m, result.Value[0].OpenPrice);
        Assert.Equal(4450m, result.Value[0].HighPrice);
        Assert.Equal(4390m, result.Value[0].LowPrice);
        Assert.Equal(4420m, result.Value[0].ClosePrice);
    }
}
