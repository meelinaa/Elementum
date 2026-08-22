using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Domain.Ports.Outbound;
using Elementum.Worker.Logging;
using Elementum.Worker.Observability;
using Microsoft.Extensions.Logging;

namespace Elementum.Worker.Jobs;

/// <summary>
/// Orchestrates a single run of metals price ingestion using the <see cref="IIngestPricesUseCase"/>.
/// Uses <see cref="IDistributedLockProvider"/> to prevent duplicate concurrent runs in multi-instance environments.
/// Uses <see cref="MetalsIngestionJobLogMessages"/> for zero-allocation logging.
/// </summary>
public class MetalsIngestionJob
{
    private const string IngestionLockResource = "lock:job:metals_ingestion";
    private const int DefaultPricesSavedBatchCount = 8; // 4 metals (Gold, Silver, Platinum, Palladium) * 2 currencies (USD, EUR)
    private static readonly TimeSpan DistributedLockTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<MetalsIngestionJob> _logger;
    private readonly IIngestPricesUseCase _ingestPricesUseCase;
    private readonly IngestionMetrics _metrics;
    private readonly IDistributedLockProvider _lockProvider;

    public MetalsIngestionJob(
        ILogger<MetalsIngestionJob> logger,
        IIngestPricesUseCase ingestPricesUseCase,
        IngestionMetrics metrics,
        IDistributedLockProvider lockProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(ingestPricesUseCase);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(lockProvider);

        _logger = logger;
        _ingestPricesUseCase = ingestPricesUseCase;
        _metrics = metrics;
        _lockProvider = lockProvider;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await _lockProvider.TryAcquireLockAsync(
            IngestionLockResource,
            DistributedLockTimeout,
            cancellationToken);

        if (!lockHandle.IsAcquired)
        {
            MetalsIngestionJobLogMessages.JobSkippedLockHeld(_logger, IngestionLockResource);
            return;
        }

        _metrics.RecordRun();
        MetalsIngestionJobLogMessages.JobStarted(_logger);
        try
        {
            await _ingestPricesUseCase.ExecuteAsync(cancellationToken);
            _metrics.RecordPricesSaved(DefaultPricesSavedBatchCount);
            MetalsIngestionJobLogMessages.JobCompleted(_logger);
        }
        catch (Exception ex)
        {
            _metrics.RecordError(ex.GetType().Name);
            MetalsIngestionJobLogMessages.JobFailed(_logger, ex);
            throw;
        }
    }
}
