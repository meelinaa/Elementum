using Elementum.Domain.Ports;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.UseCases.Ingestion;

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
        _logger.LogInformation("Checking if prices were already ingested today...");

        if (await _repository.IsDataAlreadyIngestedToday(cancellationToken))
        {
            _logger.LogInformation("Data already ingested today. Skipping ingestion.");
            return;
        }

        _logger.LogInformation("Fetching daily prices from metals API...");
        var prices = await _apiClient.GetPricesAsync(cancellationToken);

        if (prices == null || prices.Count == 0)
        {
            _logger.LogWarning("No price data returned from external API.");
            return;
        }

        _logger.LogInformation("Saving {Count} metal price records...", prices.Count);
        await _repository.SavePricesAsync(prices, cancellationToken);
        _logger.LogInformation("Price ingestion completed successfully.");
    }
}
