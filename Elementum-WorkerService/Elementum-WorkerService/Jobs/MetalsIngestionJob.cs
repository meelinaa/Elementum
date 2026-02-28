using Elementum_WorkerService.Abstractions;

namespace Elementum_WorkerService.Jobs;

/// <summary>
/// Orchestrates a single run of metals price ingestion: checks preconditions, fetches prices from the API, and persists them to the database.
/// </summary>
public class MetalsIngestionJob
{
    private readonly ILogger<MetalsIngestionJob> _logger;
    private readonly IMetalsApiClient _apiClient;
    private readonly IDatabaseCheckService _databaseCheck;
    private readonly IPriceHistoryRepository _repository;

    /// <summary>
    /// Initializes the job with its dependencies (API client, database checks, repository).
    /// </summary>
    public MetalsIngestionJob(
        ILogger<MetalsIngestionJob> logger,
        IMetalsApiClient apiClient,
        IDatabaseCheckService databaseCheck,
        IPriceHistoryRepository repository)
    {
        _logger = logger;
        _apiClient = apiClient;
        _databaseCheck = databaseCheck;
        _repository = repository;
    }

    /// <summary>
    /// Runs the ingestion pipeline: verifies database is available and no data exists for today, then fetches prices and saves them.
    /// Exits early without error if any precondition fails or no prices are returned.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Metals ingestion job started.");
        if (!await _databaseCheck.IsDatabaseAvailableAsync(cancellationToken))
            return;
        if (await _databaseCheck.DataExistsForTodayAsync(cancellationToken))
            return;

        var prices = await _apiClient.GetPricesAsync(cancellationToken);
        if (prices.Count == 0)
            return;

        await _repository.SavePricesAsync(prices, cancellationToken);
        _logger.LogInformation("Metals ingestion job completed.");
    }
}
