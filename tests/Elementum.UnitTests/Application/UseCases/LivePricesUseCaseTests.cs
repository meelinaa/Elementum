using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Models;
using Elementum.Application.Services;
using Elementum.Application.Exceptions;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Moq;

namespace Elementum.UnitTests.Application.UseCases;

public class LivePricesUseCaseTests
{
    private readonly Mock<ILiveQuotesProvider> _quotesProviderMock = new();
    private readonly Mock<IPriceHistoryReadRepository> _repositoryMock = new();
    private readonly LivePricesUseCase _useCase;

    public LivePricesUseCaseTests()
    {
        _useCase = new LivePricesUseCase(_quotesProviderMock.Object, _repositoryMock.Object);
    }

    // [R]IGHT-BICEP: Verifies that GetLiveMarketOverviewAsync transforms live quotes into an overview for all metals
    [Fact]
    public async Task GetLiveMarketOverviewAsync_ReturnsAllMetalsWithQuotes()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            SilberUsd = 30m,
            SilberEur = 27m,
            PlatinUsd = 1000m,
            PlatinEur = 900m,
            PalladiumUsd = 1100m,
            PalladiumEur = 1000m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());

        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>());

        // Act
        var result = await _useCase.GetLiveMarketOverviewAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Items.Count);
        Assert.Equal(1.15m, result.ExchangeRateUsdEur);

        var gold = result.Items.First(i => i.Symbol == "XAU");
        Assert.Equal(2500m, gold.PriceUsd);
        Assert.Equal(2300m, gold.PriceEur);
    }

    // [E]RROR RIGHT-BICEP: unknown trading symbol returns null so controller can emit 404
    [Fact]
    public async Task GetLiveTradingAnalysisAsync_WhenMetalNotFound_ReturnsNull()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse { Timestamp = 1000 };
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());

        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>());

        // Act
        var result = await _useCase.GetLiveTradingAnalysisAsync("UNKNOWN", "EUR", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    // [R]IGHT-BICEP: known symbol returns trading DTO with calculated price and symbol metadata
    [Fact]
    public async Task GetLiveTradingAnalysisAsync_WhenMetalFound_ReturnsTradingPriceDto()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2300m,
            SilberUsd = 30m,
            SilberEur = 27m,
            PlatinUsd = 1000m,
            PlatinEur = 900m,
            PalladiumUsd = 1100m,
            PalladiumEur = 1000m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _repositoryMock.Setup(r => r.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PriceHistory>());

        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>());

        // Act
        var result = await _useCase.GetLiveTradingAnalysisAsync("XAU", "EUR", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("XAU", result.Symbol);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(2300m, result.Price);
    }

    // [E]RROR RIGHT-BICEP: provider failures propagate unchanged to API exception handler boundary
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenProviderThrows_PropagatesException()
    {
        // Arrange
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(ExternalApiException.EmptyLiveQuoteResponse());

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiException>(() => _useCase.GetLiveMarketOverviewAsync(CancellationToken.None));
    }
}
