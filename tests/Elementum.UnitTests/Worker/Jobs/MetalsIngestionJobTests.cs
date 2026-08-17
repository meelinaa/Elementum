using Elementum.Application.UseCases.Ingestion;
using Elementum.Worker.Jobs;
using Elementum.Worker.Observability;
using Microsoft.Extensions.Logging;
using Moq;

namespace Elementum.Worker.Tests.Jobs;

public class MetalsIngestionJobTests
{
    private readonly Mock<ILogger<MetalsIngestionJob>> _loggerMock = new();
    private readonly Mock<IIngestPricesUseCase> _useCaseMock = new();
    private readonly IngestionMetrics _metrics = new();
    private readonly MetalsIngestionJob _job;

    public MetalsIngestionJobTests()
    {
        _job = new MetalsIngestionJob(_loggerMock.Object, _useCaseMock.Object, _metrics);
    }

    [Fact]
    public async Task RunAsync_ExecutesUseCase_Successfully()
    {
        _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _job.RunAsync(CancellationToken.None);

        _useCaseMock.Verify(u => u.ExecuteAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenUseCaseFails_RethrowsException()
    {
        _useCaseMock.Setup(u => u.ExecuteAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("API Error"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.RunAsync(CancellationToken.None));
    }
}
