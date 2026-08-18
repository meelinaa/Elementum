using Elementum.Application.Inbound.UseCases.Ingestion;
using Elementum.Domain.Ports.Outbound;
using Elementum.Worker.Observability;
using Microsoft.Extensions.Logging;

namespace Elementum.Worker.Jobs;

/// <summary>
/// Orchestrates a single run of metals price ingestion using the <see cref="IIngestPricesUseCase"/>.
/// Uses <see cref="IDistributedLockProvider"/> to prevent duplicate concurrent runs in multi-instance environments.
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ingestPricesUseCase = ingestPricesUseCase ?? throw new ArgumentNullException(nameof(ingestPricesUseCase));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _lockProvider = lockProvider ?? throw new ArgumentNullException(nameof(lockProvider));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        await using var lockHandle = await _lockProvider.TryAcquireLockAsync(
            IngestionLockResource,
            DistributedLockTimeout,
            cancellationToken);

        if (!lockHandle.IsAcquired)
        {
            _logger.LogWarning("Ingestion job skipped: another worker instance currently holds distributed lock '{Resource}'.", IngestionLockResource);
            return;
        }

        _metrics.RecordRun();
        _logger.LogInformation("Metals ingestion job started (Distributed lock acquired).");
        try
        {
            await _ingestPricesUseCase.ExecuteAsync(cancellationToken);
            _metrics.RecordPricesSaved(DefaultPricesSavedBatchCount);
            _logger.LogInformation("Metals ingestion job completed.");
        }
        catch (Exception ex)
        {
            _metrics.RecordError(ex.GetType().Name);
            _logger.LogError(ex, "Metals ingestion job failed.");
            throw;
        }
    }
}
