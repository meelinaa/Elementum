using Elementum.Application.Options;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Application.Inbound.UseCases.Ingestion;

/// <summary>
/// Interactor / Implementation for ingesting daily precious metal prices.
/// Defensively validates upstream schemas with FluentValidation before persisting.
/// </summary>
public class IngestPricesUseCase : IIngestPricesUseCase
{
    private readonly IMetalsApiClient _apiClient;
    private readonly IPriceHistoryWriteRepository _repository;
    private readonly IValidator<EdelmetalleApiResponse> _validator;
    private readonly WorkerScheduleOptions _options;
    private readonly ILogger<IngestPricesUseCase> _logger;

    public IngestPricesUseCase(
        IMetalsApiClient apiClient,
        IPriceHistoryWriteRepository repository,
        IValidator<EdelmetalleApiResponse> validator,
        IOptions<WorkerScheduleOptions> options,
        ILogger<IngestPricesUseCase> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _options = options?.Value ?? new WorkerScheduleOptions();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching precious metal prices from Edelmetalle API...");
        var edelmetalleData = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);

        if (edelmetalleData != null)
        {
            var validationResult = await _validator.ValidateAsync(edelmetalleData, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogError("Upstream Edelmetalle API data validation failed: {Errors}. Aborting persistence to protect database integrity.",
                    string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return;
            }

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

        // Daily candle consolidation & retention cleanup (runs at close hour >= DailyRollupHour)
        if (DateTime.UtcNow.Hour >= _options.DailyRollupHour)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            _logger.LogInformation("Aggregating daily {RollupHour}:00 candle summary for {Today}...", _options.DailyRollupHour, today);
            await _repository.AggregateDailySummaryAsync(today, cancellationToken);

            var retentionDays = _options.RetentionDays <= 0 ? 7 : _options.RetentionDays;
            var retentionThreshold = DateTime.UtcNow.AddDays(-retentionDays);
            _logger.LogInformation("Pruning hourly raw ticks older than {Threshold} ({RetentionDays}-day retention policy)...", retentionThreshold, retentionDays);
            var prunedCount = await _repository.PruneHourlyDataOlderThanAsync(retentionThreshold, cancellationToken);
            _logger.LogInformation("Retention cleanup completed: {Count} old hourly records purged.", prunedCount);
        }
    }
}
