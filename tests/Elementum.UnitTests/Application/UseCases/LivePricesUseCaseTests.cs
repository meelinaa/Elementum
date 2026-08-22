using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Models;
using Elementum.Application.Services;
using Elementum.Application.Exceptions;
using Elementum.Domain.Constants;
using Elementum.Domain.Entities;
using Elementum.Domain.Exceptions;
using Elementum.Domain.Ports.Outbound;
using Elementum.Domain.Services;
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
        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>());
    }

    private static EdelmetalleApiResponse FullQuote() => new()
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

    // [R]IGHT-BICEP: Verifies that GetLiveMarketOverviewAsync transforms live quotes into an overview for all metals
    [Fact]
    public async Task GetLiveMarketOverviewAsync_ReturnsAllMetalsWithQuotes()
    {
        // Arrange
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullQuote());

        // Act
        var result = await _useCase.GetLiveMarketOverviewAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Items.Count);
        Assert.Equal(1.15m, result.ExchangeRateUsdEur);

        var gold = result.Items.First(i => i.Symbol == "XAU");
        Assert.Equal(2500m, gold.PriceUsd);
        Assert.Equal(2300m, gold.PriceEur);
        _repositoryMock.Verify(
            r => r.GetDailySummariesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // [R]IGHT-BICEP: session candles from the single query drive open/high/low/prev-close and domain Chp
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenSessionCandlesExist_AppliesOhlcAndCalculator()
    {
        // Arrange
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var gold = new Metals { Id = 1, Symbol = "XAU", Name = "Gold" };

        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullQuote());

        _repositoryMock.Setup(r => r.GetDailySummariesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPriceSummary>
            {
                DailyPriceSummary.Create(1, "USD", today, 2400m, 2550m, 2380m, 2490m, metal: gold),
                DailyPriceSummary.Create(1, "USD", yesterday, 2300m, 2410m, 2290m, 2450m, metal: gold)
            });

        // Act
        var result = await _useCase.GetLiveMarketOverviewAsync(CancellationToken.None);

        // Assert
        var item = result.Items.First(i => i.Symbol == "XAU");
        var expected = TradingAnalysisCalculator.Calculate(2500m, 2400m, 2550m, 2380m, 2450m);
        Assert.Equal(2400m, item.OpenPriceUsd);
        Assert.Equal(2550m, item.HighPriceUsd);
        Assert.Equal(2380m, item.LowPriceUsd);
        Assert.Equal(2450m, item.PrevCloseUsd);
        Assert.Equal(expected.Chp, item.ChpUsd);
    }

    // RIGHT-BIC[E]P: unknown trading symbol returns null so controller can emit 404
    [Fact]
    public async Task GetLiveTradingAnalysisAsync_WhenMetalNotFound_ReturnsNull()
    {
        // Arrange
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EdelmetalleApiResponse { Timestamp = 1000 });

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
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(FullQuote());

        // Act
        var result = await _useCase.GetLiveTradingAnalysisAsync("XAU", "EUR", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("XAU", result.Symbol);
        Assert.Equal("EUR", result.Currency);
        Assert.Equal(2300m, result.Price);
        Assert.Equal(DomainConstants.Trading.BullishStatus, result.Status);
    }

    // RIGHT-BIC[E]P: unsupported ISO codes fail in the domain instead of silently falling back to EUR
    [Fact]
    public async Task GetLiveTradingAnalysisAsync_WhenCurrencyUnsupported_ThrowsUnsupportedCurrencyException()
    {
        await Assert.ThrowsAsync<UnsupportedCurrencyException>(
            () => _useCase.GetLiveTradingAnalysisAsync("XAU", "GBP", CancellationToken.None));
        _quotesProviderMock.Verify(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // RIGHT-BIC[E]P: provider failures propagate unchanged to API exception handler boundary
    [Fact]
    public async Task GetLiveMarketOverviewAsync_WhenProviderThrows_PropagatesException()
    {
        // Arrange
        _quotesProviderMock.Setup(q => q.GetLiveQuoteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(ExternalApiException.EmptyLiveQuoteResponse());

        // Act & Assert
        await Assert.ThrowsAsync<ExternalApiException>(() => _useCase.GetLiveMarketOverviewAsync(CancellationToken.None));
        _repositoryMock.Verify(
            r => r.GetDailySummariesAsync(It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
