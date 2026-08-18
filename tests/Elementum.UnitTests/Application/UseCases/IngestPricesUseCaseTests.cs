using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Options;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elementum.Application.Tests.UseCases;

public class IngestPricesUseCaseTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly Mock<IPriceHistoryRepository> _repositoryMock = new();
    private readonly Mock<ILogger<IngestPricesUseCase>> _loggerMock = new();
    private readonly WorkerScheduleOptions _options = new() { DailyRollupHour = 0, RetentionDays = 14 };
    private readonly IngestPricesUseCase _useCase;

    public IngestPricesUseCaseTests()
    {
        _useCase = new IngestPricesUseCase(
            _apiClientMock.Object,
            _repositoryMock.Object,
            Microsoft.Extensions.Options.Options.Create(_options),
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleReturnsData_SavesEdelmetallePrices()
    {
        var response = new EdelmetalleApiResponse
        {
            GoldUsd = 4410.6m,
            GoldEur = 3803.8m,
            SilberUsd = 65.7m,
            SilberEur = 56.6m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.SaveEdelmetallePricesAsync(response, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleNull_FallsBackToGetPricesAsync()
    {
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);

        var fallbackPrices = new List<DailyPrices>
        {
            new() { Metal = "Gold", Price = 2500m, Symbol = "XAU" }
        };

        _apiClientMock.Setup(c => c.GetPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackPrices);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.SavePricesAsync(fallbackPrices, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenHourExceedsDailyRollupHour_PerformsRollupAndRetentionPruning()
    {
        var response = new EdelmetalleApiResponse
        {
            GoldUsd = 4400m,
            GoldEur = 3800m,
            SilberUsd = 65m,
            SilberEur = 56m,
            Timestamp = 1786975085,
            WechselkursUsdEur = 1.15m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _repositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
