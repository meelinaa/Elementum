using Elementum.Application.Exceptions;
using Elementum.Application.Logging;
using Elementum.Application.Mapping;
using Elementum.Application.Models;
using Elementum.Application.Options;
using Elementum.Application.Ports.Outbound;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Application.Inbound.UseCases.Ingestion;

/// <summary>
/// Interactor / Implementation for ingesting daily precious metal prices.
/// Defensively validates upstream schemas with FluentValidation, maps to domain entities, and persists.
/// Uses zero-allocation, source-generated <see cref="IngestionLogMessages"/>.
/// </summary>
public class IngestPricesUseCase : IIngestPricesUseCase
{
    private readonly IMetalsApiClient _apiClient;
    private readonly IPriceHistoryWriteRepository _writeRepository;
    private readonly IPriceHistoryReadRepository _readRepository;
    private readonly IValidator<EdelmetalleApiResponse> _validator;
    private readonly WorkerScheduleOptions _options;
    private readonly ILogger<IngestPricesUseCase> _logger;

    public IngestPricesUseCase(
        IMetalsApiClient apiClient,
        IPriceHistoryWriteRepository writeRepository,
        IPriceHistoryReadRepository readRepository,
        IValidator<EdelmetalleApiResponse> validator,
        IOptions<WorkerScheduleOptions> options,
        ILogger<IngestPricesUseCase> logger)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(writeRepository);
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(logger);

        _apiClient = apiClient;
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _validator = validator;
        _options = options?.Value ?? new WorkerScheduleOptions();
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        IngestionLogMessages.FetchingPrices(_logger);
        var edelmetalleData = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var metals = (await _readRepository.GetMetalsAsync(cancellationToken))
            .ToDictionary(m => m.Symbol, m => m.Id, StringComparer.OrdinalIgnoreCase);

        if (edelmetalleData != null)
        {
            var validationResult = await _validator.ValidateAsync(edelmetalleData, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                IngestionLogMessages.UpstreamValidationFailed(_logger, errors);
                throw UpstreamValidationException.FromErrors(errors);
            }

            var priceEntities = edelmetalleData.ToPriceHistoryEntities(metals, today);
            IngestionLogMessages.SavingHourlyQuotes(_logger);
            await _writeRepository.SavePricesAsync(priceEntities, cancellationToken);
            IngestionLogMessages.HourlyIngestionSuccess(_logger);
            await EnsureCatalogCompleteAsync(cancellationToken);
        }
        else if (await _readRepository.IsDataAlreadyIngestedToday(cancellationToken))
        {
            IngestionLogMessages.SkippingFallbackCatalogComplete(_logger);
        }
        else
        {
            var prices = await _apiClient.GetPricesAsync(cancellationToken);
            if (prices != null && prices.Count > 0)
            {
                var priceEntities = prices.ToPriceHistoryEntities(metals, today);
                IngestionLogMessages.SavingFallbackPrices(_logger, priceEntities.Count);
                await _writeRepository.SavePricesAsync(priceEntities, cancellationToken);
                await EnsureCatalogCompleteAsync(cancellationToken);
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
            IngestionLogMessages.AggregatingDailyCandle(_logger, _options.DailyRollupHour, today);
            await _writeRepository.AggregateDailySummaryAsync(today, cancellationToken);

            var retentionDays = _options.RetentionDays <= 0 ? 7 : _options.RetentionDays;
            var retentionThreshold = DateTime.UtcNow.AddDays(-retentionDays);
            IngestionLogMessages.PruningHourlyTicks(_logger, retentionThreshold, retentionDays);
            var prunedCount = await _writeRepository.PruneHourlyDataOlderThanAsync(retentionThreshold, cancellationToken);
            IngestionLogMessages.RetentionCleanupCompleted(_logger, prunedCount);
        }
    }

    private async Task EnsureCatalogCompleteAsync(CancellationToken cancellationToken)
    {
        if (!await _readRepository.IsDataAlreadyIngestedToday(cancellationToken))
            throw ExternalApiException.IncompleteDailyCatalog();
    }
}
