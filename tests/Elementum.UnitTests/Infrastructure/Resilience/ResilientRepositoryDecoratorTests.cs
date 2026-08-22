using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.Outbound.Data.Resilience;
using Moq;
using Polly;

namespace Elementum.UnitTests.Infrastructure.Resilience;

public class ResilientRepositoryDecoratorTests
{
    private readonly ResiliencePipeline _noopPipeline = new ResiliencePipelineBuilder().Build();

    // [R]IGHT-BICEP: Read decorator forwards GetMetalsAsync to inner repository through resilience pipeline
    [Fact]
    public async Task ResilientReadRepository_GetMetalsAsync_DelegatesToInner()
    {
        // Arrange
        var mock = new Mock<IPriceHistoryReadRepository>();
        var expected = new List<Metals> { new() { Id = 1, Symbol = "XAU", Name = "Gold" } };
        mock.Setup(x => x.GetMetalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var decorator = new ResilientPriceHistoryReadRepository(mock.Object, _noopPipeline);

        // Act
        var result = await decorator.GetMetalsAsync();

        // Assert
        Assert.Same(expected, result);
        mock.Verify(x => x.GetMetalsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Read decorator forwards GetDailySummariesAsync to inner repository
    [Fact]
    public async Task ResilientReadRepository_GetDailySummariesAsync_DelegatesToInner()
    {
        // Arrange
        var mock = new Mock<IPriceHistoryReadRepository>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expected = new List<DailyPriceSummary>();
        mock.Setup(x => x.GetDailySummariesAsync(today, today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var decorator = new ResilientPriceHistoryReadRepository(mock.Object, _noopPipeline);

        // Act
        var result = await decorator.GetDailySummariesAsync(today, today);

        // Assert
        Assert.Same(expected, result);
        mock.Verify(x => x.GetDailySummariesAsync(today, today, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Write decorator forwards SavePricesAsync to inner repository through resilience pipeline
    [Fact]
    public async Task ResilientWriteRepository_SavePricesAsync_DelegatesToInner()
    {
        // Arrange
        var mock = new Mock<IPriceHistoryWriteRepository>();
        var prices = new List<PriceHistory>
        {
            PriceHistory.Create(1, "USD", DateOnly.FromDateTime(DateTime.UtcNow), 2500m, "XAU")
        };
        mock.Setup(x => x.SavePricesAsync(prices, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var decorator = new ResilientPriceHistoryWriteRepository(mock.Object, _noopPipeline);

        // Act
        await decorator.SavePricesAsync(prices);

        // Assert
        mock.Verify(x => x.SavePricesAsync(prices, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Write decorator forwards PruneHourlyDataOlderThanAsync to inner repository
    [Fact]
    public async Task ResilientWriteRepository_PruneHourlyDataOlderThanAsync_DelegatesToInner()
    {
        // Arrange
        var mock = new Mock<IPriceHistoryWriteRepository>();
        var threshold = DateTime.UtcNow.AddDays(-7);
        mock.Setup(x => x.PruneHourlyDataOlderThanAsync(threshold, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        var decorator = new ResilientPriceHistoryWriteRepository(mock.Object, _noopPipeline);

        // Act
        var deleted = await decorator.PruneHourlyDataOlderThanAsync(threshold);

        // Assert
        Assert.Equal(42, deleted);
        mock.Verify(x => x.PruneHourlyDataOlderThanAsync(threshold, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Composite ResilientElementumDbContext forwards to segregated read and write decorators
    [Fact]
    public async Task ResilientElementumDbContext_ComposedDelegation_OperatesCorrectly()
    {
        // Arrange
        var readMock = new Mock<IPriceHistoryReadRepository>();
        var writeMock = new Mock<IPriceHistoryWriteRepository>();

        readMock.Setup(x => x.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        writeMock.Setup(x => x.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var composite = new ResilientElementumDbContext(readMock.Object, writeMock.Object);

        // Act
        var isIngested = await composite.IsDataAlreadyIngestedToday();
        await composite.AggregateDailySummaryAsync(DateOnly.FromDateTime(DateTime.UtcNow));

        // Assert
        Assert.True(isIngested);
        readMock.Verify(x => x.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()), Times.Once);
        writeMock.Verify(x => x.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
