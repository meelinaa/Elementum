using Elementum.Application.Logging;
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
/// Uses zero-allocation, source-generated <see cref="IngestionLogMessages"/>.
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
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _apiClient = apiClient;
        _repository = repository;
        _validator = validator;
        _options = options?.Value ?? new WorkerScheduleOptions();
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IngestionLogMessages.FetchingPrices(_logger);
        var edelmetalleData = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);

        if (edelmetalleData != null)
        {
            var validationResult = await _validator.ValidateAsync(edelmetalleData, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                IngestionLogMessages.UpstreamValidationFailed(_logger, errors);
                return;
            }

            IngestionLogMessages.SavingHourlyQuotes(_logger);
            await _repository.SaveEdelmetallePricesAsync(edelmetalleData, cancellationToken);
            IngestionLogMessages.HourlyIngestionSuccess(_logger);
        }
        else
        {
            // Fallback to general GetPricesAsync
            var prices = await _apiClient.GetPricesAsync(cancellationToken);
            if (prices != null && prices.Count > 0)
            {
                IngestionLogMessages.SavingFallbackPrices(_logger, prices.Count);
                await _repository.SavePricesAsync(prices, cancellationToken);
            }
            else
            {
                IngestionLogMessages.NoPriceDataReturned(_logger);
                return;
            }
        }

        // Daily candle consolidation & retention cleanup (runs at close hour >= DailyRollupHour)
        if (DateTime.UtcNow.Hour >= _options.DailyRollupHour)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            IngestionLogMessages.AggregatingDailyCandle(_logger, _options.DailyRollupHour, today);
            await _repository.AggregateDailySummaryAsync(today, cancellationToken);

            var retentionDays = _options.RetentionDays <= 0 ? 7 : _options.RetentionDays;
            var retentionThreshold = DateTime.UtcNow.AddDays(-retentionDays);
            IngestionLogMessages.PruningHourlyTicks(_logger, retentionThreshold, retentionDays);
            var prunedCount = await _repository.PruneHourlyDataOlderThanAsync(retentionThreshold, cancellationToken);
            IngestionLogMessages.RetentionCleanupCompleted(_logger, prunedCount);
        }
    }
}
