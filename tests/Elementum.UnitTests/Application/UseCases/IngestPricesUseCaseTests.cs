using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Options;
using Elementum.Application.Validation;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elementum.Application.Tests.UseCases;

public class IngestPricesUseCaseTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly Mock<IPriceHistoryWriteRepository> _repositoryMock = new();
    private readonly Mock<ILogger<IngestPricesUseCase>> _loggerMock = new();
    private readonly WorkerScheduleOptions _options = new() { DailyRollupHour = 0, RetentionDays = 14 };
    private readonly EdelmetalleApiResponseValidator _validator = new();
    private readonly IngestPricesUseCase _useCase;

    public IngestPricesUseCaseTests()
    {
        _useCase = new IngestPricesUseCase(
            _apiClientMock.Object,
            _repositoryMock.Object,
            _validator,
            Microsoft.Extensions.Options.Options.Create(_options),
            _loggerMock.Object);
    }

    // [R]IGHT-BICEP: Verifies that valid metal quotes from the primary API endpoint are saved to repository
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleReturnsValidData_SavesEdelmetallePrices()
    {
        // Arrange
        var response = new EdelmetalleApiResponse
        {
            GoldUsd = 2500.6m,
            GoldEur = 2280.8m,
            SilberUsd = 30.7m,
            SilberEur = 28.6m,
            PlatinUsd = 1000m,
            PlatinEur = 910m,
            PalladiumUsd = 1050m,
            PalladiumEur = 960m,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WechselkursUsdEur = 1.095m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.SaveEdelmetallePricesAsync(response, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [E]RROR: Verifies that validation failure on corrupted payload aborts persistence
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleReturnsInvalidData_AbortsAndDoesNotSave()
    {
        // Arrange
        var invalidResponse = new EdelmetalleApiResponse
        {
            GoldUsd = -100m,
            Timestamp = 0,
            WechselkursUsdEur = 0m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.SaveEdelmetallePricesAsync(It.IsAny<EdelmetalleApiResponse>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [B]OUNDARY / FALLBACK: Verifies that null primary payload triggers fallback to secondary GetPricesAsync
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleNull_FallsBackToGetPricesAsync()
    {
        // Arrange
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);

        var fallbackPrices = new List<DailyPrices>
        {
            new() { Metal = "Gold", Price = 2500m, Symbol = "XAU" }
        };

        _apiClientMock.Setup(c => c.GetPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(fallbackPrices);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.SavePricesAsync(fallbackPrices, It.IsAny<CancellationToken>()), Times.Once);
    }

    // [R]IGHT-BICEP: Verifies that the daily rollup and retention pruning trigger at configured threshold hour
    [Fact]
    public async Task ExecuteAsync_WhenHourExceedsDailyRollupHour_PerformsRollupAndRetentionPruning()
    {
        // Arrange
        var response = new EdelmetalleApiResponse
        {
            GoldUsd = 2500m,
            GoldEur = 2280m,
            SilberUsd = 30m,
            SilberEur = 28m,
            PlatinUsd = 1000m,
            PlatinEur = 910m,
            PalladiumUsd = 1050m,
            PalladiumEur = 960m,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WechselkursUsdEur = 1.095m
        };

        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _repositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
