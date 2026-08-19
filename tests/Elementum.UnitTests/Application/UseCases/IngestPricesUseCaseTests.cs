using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Application.Models;
using Elementum.Application.Options;
using Elementum.Application.Ports.Outbound;
using Elementum.Application.Validation;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Elementum.Application.Tests.UseCases;

public class IngestPricesUseCaseTests
{
    private readonly Mock<IMetalsApiClient> _apiClientMock = new();
    private readonly Mock<IPriceHistoryWriteRepository> _writeRepositoryMock = new();
    private readonly Mock<IPriceHistoryReadRepository> _readRepositoryMock = new();
    private readonly Mock<ILogger<IngestPricesUseCase>> _loggerMock = new();
    private readonly WorkerScheduleOptions _options = new() { DailyRollupHour = 0, RetentionDays = 14 };
    private readonly EdelmetalleApiResponseValidator _validator = new();
    private readonly IngestPricesUseCase _useCase;

    public IngestPricesUseCaseTests()
    {
        var metals = new List<Metals>
        {
            new() { Id = 1, Symbol = "XAU", Name = "Gold" },
            new() { Id = 2, Symbol = "XAG", Name = "Silver" },
            new() { Id = 3, Symbol = "XPT", Name = "Platinum" },
            new() { Id = 4, Symbol = "XPD", Name = "Palladium" }
        }.AsQueryable();

        _readRepositoryMock.Setup(r => r.QueryMetals()).Returns(metals);

        _useCase = new IngestPricesUseCase(
            _apiClientMock.Object,
            _writeRepositoryMock.Object,
            _readRepositoryMock.Object,
            _validator,
            Microsoft.Extensions.Options.Options.Create(_options),
            _loggerMock.Object);
    }

    // [R]IGHT-BICEP: Verifies that valid metal quotes from the primary API endpoint are mapped and saved to repository
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
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.Is<IReadOnlyList<PriceHistory>>(l => l.Count == 8), It.IsAny<CancellationToken>()), Times.Once);
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
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.Is<IReadOnlyList<PriceHistory>>(l => l.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
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
        _writeRepositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _writeRepositoryMock.Verify(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // [B]OUNDARY RIGHT-BICEP: DailyRollupHour above 23 skips rollup/prune even after successful tick save (crash-between-steps scenario)
    [Fact]
    public async Task ExecuteAsync_WhenRollupHourNotReached_SavesTicksWithoutRollupOrPrune()
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

        var skipRollupOptions = Microsoft.Extensions.Options.Options.Create(new WorkerScheduleOptions
        {
            DailyRollupHour = 99,
            RetentionDays = 14
        });

        var useCaseWithoutRollup = new IngestPricesUseCase(
            _apiClientMock.Object,
            _writeRepositoryMock.Object,
            _readRepositoryMock.Object,
            _validator,
            skipRollupOptions,
            _loggerMock.Object);

        // Act
        await useCaseWithoutRollup.ExecuteAsync(CancellationToken.None);

        // Assert
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Once);
        _writeRepositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
        _writeRepositoryMock.Verify(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // [I]NVERSE RIGHT-BICEP: after a partial run (ticks saved, rollup skipped), the next run completes rollup and prune
    [Fact]
    public async Task ExecuteAsync_WhenPriorRunSavedTicksOnly_SubsequentRunCompletesRollupAndPrune()
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

        var skipRollupOptions = Microsoft.Extensions.Options.Options.Create(new WorkerScheduleOptions
        {
            DailyRollupHour = 99,
            RetentionDays = 14
        });

        var useCaseWithoutRollup = new IngestPricesUseCase(
            _apiClientMock.Object,
            _writeRepositoryMock.Object,
            _readRepositoryMock.Object,
            _validator,
            skipRollupOptions,
            _loggerMock.Object);

        await useCaseWithoutRollup.ExecuteAsync(CancellationToken.None);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _writeRepositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Once);
        _writeRepositoryMock.Verify(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
