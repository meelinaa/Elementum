using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Models;
using Elementum.Application.Services;
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

        _repositoryMock.Setup(r => r.QueryPriceHistoryByMetalSymbolAndDateRange(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new List<PriceHistory>().AsQueryable());

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

    // [B]OUNDARY / [E]RROR: Verifies that GetLiveTradingAnalysisAsync returns null when querying an unknown metal symbol
    [Fact]
    public async Task GetLiveTradingAnalysisAsync_WhenMetalNotFound_ReturnsNull()
    {
        // Arrange
        var quote = new EdelmetalleApiResponse { Timestamp = 1000 };
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(quote);

        _repositoryMock.Setup(r => r.QueryPriceHistoryByMetalSymbolAndDateRange(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .Returns(new List<PriceHistory>().AsQueryable());

        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>());

        // Act
        var result = await _useCase.GetLiveTradingAnalysisAsync("UNKNOWN", "EUR", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
