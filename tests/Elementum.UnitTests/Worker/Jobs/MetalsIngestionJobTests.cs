using System.Diagnostics.Metrics;
using Elementum.Application.Exceptions;
using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Domain.Ports.Outbound;
using Elementum.Worker.Jobs;
using Elementum.Worker.Observability;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.UnitTests.Worker.Jobs;

public class MetalsIngestionJobTests
{
    private readonly Mock<ILogger<MetalsIngestionJob>> _loggerMock = new();
    private readonly Mock<IIngestPricesUseCase> _useCaseMock = new();
    private readonly IngestionMetrics _metrics = new();
    private readonly Mock<IDistributedLockProvider> _lockProviderMock = new();

    // [R]IGHT-BICEP: Verifies that MetalsIngestionJob executes use case and releases lock when acquired
    [Fact]
    public async Task RunAsync_WhenLockAcquired_ExecutesUseCaseSuccessfully()
    {
        // Arrange
        var lockMock = new Mock<IDistributedLock>();
        lockMock.SetupGet(l => l.IsAcquired).Returns(true);

        _lockProviderMock.Setup(p => p.TryAcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new MetalsIngestionJob(_loggerMock.Object, _useCaseMock.Object, _metrics, _lockProviderMock.Object);

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert
        _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
        lockMock.Verify(l => l.DisposeAsync(), Times.Once);
    }

    // [B]OUNDARY / CONCURRENCY: Verifies that job skips execution safely when another worker instance holds the lock
    [Fact]
    public async Task RunAsync_WhenLockAlreadyHeldByAnotherInstance_SkipsExecution()
    {
        // Arrange
        var lockMock = new Mock<IDistributedLock>();
        lockMock.SetupGet(l => l.IsAcquired).Returns(false);

        _lockProviderMock.Setup(p => p.TryAcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        var job = new MetalsIngestionJob(_loggerMock.Object, _useCaseMock.Object, _metrics, _lockProviderMock.Object);

        // Act
        await job.RunAsync(CancellationToken.None);

        // Assert
        _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Never);
        lockMock.Verify(l => l.DisposeAsync(), Times.Once);
    }

    // [E]RROR RIGHT-BICEP: use case exceptions propagate and lock handle is still disposed via await using
    [Fact]
    public async Task RunAsync_WhenUseCaseFails_RethrowsExceptionAndDisposesLock()
    {
        // Arrange
        var lockMock = new Mock<IDistributedLock>();
        lockMock.SetupGet(l => l.IsAcquired).Returns(true);

        _lockProviderMock.Setup(p => p.TryAcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API Error"));

        var job = new MetalsIngestionJob(_loggerMock.Object, _useCaseMock.Object, _metrics, _lockProviderMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => job.RunAsync(CancellationToken.None));
        Assert.Equal("API Error", ex.Message);
        lockMock.Verify(l => l.DisposeAsync(), Times.Once);
    }

    // [E]RROR RIGHT-BICEP: upstream validation failures increment the error metric and do not count saved prices
    [Fact]
    public async Task RunAsync_WhenUpstreamValidationFails_RecordsErrorAndRethrows()
    {
        var lockMock = new Mock<IDistributedLock>();
        lockMock.SetupGet(l => l.IsAcquired).Returns(true);

        _lockProviderMock.Setup(p => p.TryAcquireLockAsync(
                It.IsAny<string>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lockMock.Object);

        _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(UpstreamValidationException.FromErrors("GoldUsd must be greater than 0"));

        long errors = 0;
        long pricesSaved = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == "Elementum.Worker")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "ingestion_errors_total")
                errors += measurement;
            if (instrument.Name == "ingestion_prices_saved_total")
                pricesSaved += measurement;
        });
        listener.Start();

        var metrics = new IngestionMetrics();
        var job = new MetalsIngestionJob(_loggerMock.Object, _useCaseMock.Object, metrics, _lockProviderMock.Object);

        await Assert.ThrowsAsync<UpstreamValidationException>(() => job.RunAsync(CancellationToken.None));

        Assert.Equal(1, errors);
        Assert.Equal(0, pricesSaved);
    }
}
