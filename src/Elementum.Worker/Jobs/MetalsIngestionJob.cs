using Elementum.Application.UseCases.Ingestion;
using Elementum.Worker.Observability;
using Microsoft.Extensions.Logging;

namespace Elementum.Worker.Jobs;

/// <summary>
/// Orchestrates a single run of metals price ingestion using the <see cref="IIngestPricesUseCase"/>.
/// </summary>
public class MetalsIngestionJob
{
    private readonly ILogger<MetalsIngestionJob> _logger;
    private readonly IIngestPricesUseCase _ingestPricesUseCase;
    private readonly IngestionMetrics _metrics;

    public MetalsIngestionJob(
        ILogger<MetalsIngestionJob> logger,
        IIngestPricesUseCase ingestPricesUseCase,
        IngestionMetrics metrics)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ingestPricesUseCase = ingestPricesUseCase ?? throw new ArgumentNullException(nameof(ingestPricesUseCase));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _metrics.RecordRun();
        _logger.LogInformation("Metals ingestion job started.");
        try
        {
            await _ingestPricesUseCase.ExecuteAsync(cancellationToken);
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
