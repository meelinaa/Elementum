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

    // [E]RROR: Verifies that use case exceptions propagate through the job execution boundary
    [Fact]
    public async Task RunAsync_WhenUseCaseFails_RethrowsException()
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
        await Assert.ThrowsAsync<InvalidOperationException>(() => job.RunAsync(CancellationToken.None));
    }
}
