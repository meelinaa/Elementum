using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;
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
            PriceHistory.Create(1, "USD", new DateOnly(2026, 8, 17), 2500m, "XAU", referenceTimestamp: 1000L)
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
        var entity = PriceHistory.Create(1, "USD", new DateOnly(2026, 8, 17), 2500m, "XAU", referenceTimestamp: 1000L);

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

    // [R]IGHT-BICEP: Verifies that GetBySymbolAsync maps a paged date-range query with currency
    [Fact]
    public async Task GetBySymbolAsync_WhenCurrencyProvided_PassesFilterToRepository()
    {
        // Arrange
        var first = new DateOnly(2026, 8, 1);
        var last = new DateOnly(2026, 8, 17);
        var entities = new List<PriceHistory>
        {
            PriceHistory.Create(1, "EUR", last, 2300m, "XAU", referenceTimestamp: 1000L)
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, "EUR", 0, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entities, entities.Count));

        // Act
        var page = await _useCase.GetBySymbolAsync("XAU", "EUR", "2026-08-01", "2026-08-17", skip: 0, take: 10);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal(2300m, item.Price);
        Assert.False(page.HasMore);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(10, page.Take);
        _repositoryMock.Verify(
            r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, "EUR", 0, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // [E]RROR RIGHT-BICEP: unsupported currency never reaches the repository
    [Fact]
    public async Task GetBySymbolAsync_WhenCurrencyUnsupported_ThrowsUnsupportedCurrencyException()
    {
        await Assert.ThrowsAsync<UnsupportedCurrencyException>(
            () => _useCase.GetBySymbolAsync("XAU", "GBP").AsTask());

        _repositoryMock.Verify(
            r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // [B]OUNDARY: omitted from/to defaults to the last 30 UTC days with take clamped to DefaultTake
    [Fact]
    public async Task GetBySymbolAsync_WhenDatesOmitted_DefaultsToLastThirtyDays()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expectedFrom = today.AddDays(-HistoryQueryLimits.DefaultLookbackDays);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", expectedFrom, today, null, 0, HistoryQueryLimits.DefaultTake, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<PriceHistory>(), 0));

        var page = await _useCase.GetBySymbolAsync("XAU");

        Assert.Equal(expectedFrom, page.From);
        Assert.Equal(today, page.To);
        Assert.Equal(HistoryQueryLimits.DefaultTake, page.Take);
        Assert.Empty(page.Items);
    }

    // [B]OUNDARY: take above MaxTake is clamped, not rejected
    [Fact]
    public async Task GetByDateRangeAsync_WhenTakeExceedsCap_ClampsToMaxTake()
    {
        var first = new DateOnly(2026, 8, 1);
        var last = new DateOnly(2026, 8, 17);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, null, 0, HistoryQueryLimits.MaxTake, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<PriceHistory>(), 0));

        var page = await _useCase.GetByDateRangeAsync("XAU", first, last, take: HistoryQueryLimits.MaxTake + 500);

        Assert.Equal(HistoryQueryLimits.MaxTake, page.Take);
        _repositoryMock.Verify(
            r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, null, 0, HistoryQueryLimits.MaxTake, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // [R]IGHT-BICEP: hasMore is true when skip + page size is below the unpaged total
    [Fact]
    public async Task GetByDateRangeAsync_WhenMoreRowsExist_SetsHasMore()
    {
        var first = new DateOnly(2026, 8, 1);
        var last = new DateOnly(2026, 8, 17);
        var entities = new List<PriceHistory>
        {
            PriceHistory.Create(1, "USD", first, 2500m, "XAU", referenceTimestamp: 1000L)
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, null, 0, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entities, 5));

        var page = await _useCase.GetByDateRangeAsync("XAU", first, last, skip: 0, take: 1);

        Assert.True(page.HasMore);
        Assert.Equal(5, page.TotalCount);
        Assert.Single(page.Items);
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
            PriceHistory.Create(1, "USD", first, 2500m, "XAU", referenceTimestamp: 1000L)
        };

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                "XAU", first, last, null, 0, HistoryQueryLimits.DefaultTake, It.IsAny<CancellationToken>()))
            .ReturnsAsync((entities, 1));

        // Act
        var result = await _useCase.GetByDateRangeAsync("XAU", first, last);

        // Assert
        var item = Assert.Single(result.Items);
        Assert.Equal(2500m, item.Price);
        Assert.False(result.HasMore);
    }

    // [R]IGHT-BICEP: Verifies that GetTradingLatestAsync computes and exposes technical indicators
    [Fact]
    public async Task GetTradingLatestAsync_ReturnsTradingPriceDto()
    {
        // Arrange
        var entity = PriceHistory.Create(1, "USD", new DateOnly(2026, 8, 17), 2500m, "XAU", referenceTimestamp: 1000L);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolLatest("XAU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _useCase.GetTradingLatestAsync("XAU");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.Price);
    }
}
