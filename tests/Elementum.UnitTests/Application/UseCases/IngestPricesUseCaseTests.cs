using Elementum.Application.Exceptions;
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
        };

        _readRepositoryMock.Setup(r => r.GetMetalsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metals);
        _readRepositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

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

    // RIGHT-BIC[E]P: validation failure on corrupted payload aborts persistence and is not a silent return
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleReturnsInvalidData_ThrowsUpstreamValidationException()
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

        // Act & Assert
        await Assert.ThrowsAsync<UpstreamValidationException>(() => _useCase.ExecuteAsync(CancellationToken.None));
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // RIGHT-[B]ICEP: Verifies that null primary payload triggers fallback to secondary GetPricesAsync
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleNull_FallsBackToGetPricesAsync()
    {
        // Arrange
        _readRepositoryMock.SetupSequence(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

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

    // RIGHT-[B]ICEP: DailyRollupHour above 23 skips rollup/prune even after successful tick save (crash-between-steps scenario)
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

    // RIGHT-BIC[E]P: when both primary and fallback APIs return no data, persistence must be skipped
    [Fact]
    public async Task ExecuteAsync_WhenEdelmetalleNullAndFallbackEmpty_DoesNotSave()
    {
        // Arrange
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);
        _apiClientMock.Setup(c => c.GetPricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DailyPrices>());
        _readRepositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Never);
        _writeRepositoryMock.Verify(r => r.AggregateDailySummaryAsync(It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // RIGHT-[B]ICEP: RetentionDays <= 0 falls back to seven-day default when pruning hourly ticks
    [Fact]
    public async Task ExecuteAsync_WhenRetentionDaysZero_UsesSevenDayDefaultForPrune()
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

        var zeroRetentionOptions = Microsoft.Extensions.Options.Options.Create(new WorkerScheduleOptions
        {
            DailyRollupHour = 0,
            RetentionDays = 0
        });

        var useCase = new IngestPricesUseCase(
            _apiClientMock.Object,
            _writeRepositoryMock.Object,
            _readRepositoryMock.Object,
            _validator,
            zeroRetentionOptions,
            _loggerMock.Object);

        DateTime? capturedThreshold = null;
        _writeRepositoryMock.Setup(r => r.PruneHourlyDataOlderThanAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, CancellationToken>((threshold, _) => capturedThreshold = threshold)
            .ReturnsAsync(0);

        var expectedThreshold = DateTime.UtcNow.AddDays(-7);

        // Act
        await useCase.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(capturedThreshold);
        Assert.True(Math.Abs((capturedThreshold.Value - expectedThreshold).TotalMinutes) < 2);
    }

    // [R]IGHT-BICEP: complete catalog today skips a redundant fallback when the primary payload is empty
    [Fact]
    public async Task ExecuteAsync_WhenPrimaryNullAndCatalogComplete_SkipsFallbackFetch()
    {
        _apiClientMock.Setup(c => c.GetEdelmetallePricesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((EdelmetalleApiResponse?)null);
        _readRepositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _useCase.ExecuteAsync(CancellationToken.None);

        _apiClientMock.Verify(c => c.GetPricesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // RIGHT-BIC[E]P: a persist that does not cover every catalog metal is a countable upstream failure
    [Fact]
    public async Task ExecuteAsync_WhenSavedTicksDoNotCoverCatalog_ThrowsIncompleteDailyCatalog()
    {
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
        _readRepositoryMock.Setup(r => r.IsDataAlreadyIngestedToday(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<ExternalApiException>(() => _useCase.ExecuteAsync(CancellationToken.None));
        Assert.Contains("complete metals catalog", ex.Message, StringComparison.Ordinal);
        _writeRepositoryMock.Verify(r => r.SavePricesAsync(It.IsAny<IReadOnlyList<PriceHistory>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // RIGHT-B[I]CEP: after a partial run (ticks saved, rollup skipped), the next run completes rollup and prune
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
