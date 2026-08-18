using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Inbound.UseCases.Ingestion;

/// <summary>
/// Interactor / Implementation for ingesting daily precious metal prices.
/// </summary>
public class IngestPricesUseCase : IIngestPricesUseCase
{
    private readonly IMetalsApiClient _apiClient;
    private readonly IPriceHistoryRepository _repository;
    private readonly ILogger<IngestPricesUseCase> _logger;

    public IngestPricesUseCase(
        IMetalsApiClient apiClient,
        IPriceHistoryRepository repository,
        ILogger<IngestPricesUseCase> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching precious metal prices from Edelmetalle API...");
        var edelmetalleData = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);

        if (edelmetalleData != null)
        {
            _logger.LogInformation("Saving hourly precious metal quotes (Gold, Silber, Platin, Palladium in USD & EUR)...");
            await _repository.SaveEdelmetallePricesAsync(edelmetalleData, cancellationToken);
            _logger.LogInformation("Hourly price ingestion completed successfully.");
        }
        else
        {
            // Fallback to general GetPricesAsync
            var prices = await _apiClient.GetPricesAsync(cancellationToken);
            if (prices != null && prices.Count > 0)
            {
                _logger.LogInformation("Saving {Count} price records from fallback API...", prices.Count);
                await _repository.SavePricesAsync(prices, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No price data returned from external metals API.");
                return;
            }
        }

        // 7-day retention cleanup & daily candle consolidation (runs at close hour >= 22 or daily)
        if (DateTime.UtcNow.Hour >= 22)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            _logger.LogInformation("Aggregating daily 22:00 candle summary for {Today}...", today);
            await _repository.AggregateDailySummaryAsync(today, cancellationToken);

            var retentionThreshold = DateTime.UtcNow.AddDays(-7);
            _logger.LogInformation("Pruning hourly raw ticks older than {Threshold} (7-day retention policy)...", retentionThreshold);
            var prunedCount = await _repository.PruneHourlyDataOlderThanAsync(retentionThreshold, cancellationToken);
            _logger.LogInformation("Retention cleanup completed: {Count} old hourly records purged.", prunedCount);
        }
    }
}
