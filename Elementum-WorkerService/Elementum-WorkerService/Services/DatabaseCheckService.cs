using Elementum.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum_WorkerService.Services;

/// <summary>
/// Provides preconditions for the ingestion job: database connectivity and whether data for today already exists.
/// </summary>
public class DatabaseCheckService
{
    private readonly ILogger<DatabaseCheckService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public DatabaseCheckService(ILogger<DatabaseCheckService> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Verifies that the database is reachable via <see cref="ElementumDbContext.Database.CanConnectAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection succeeded; false otherwise (errors are logged).</returns>
    public async Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            if (!await db.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogError("Database connection test failed: Cannot connect to the database.");
                return false;
            }
            _logger.LogInformation("Database connection test passed.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database connection test failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Checks whether at least one row exists in <c>price_history</c> for today's date (UTC).
    /// Used to skip ingestion when data has already been loaded for the current day.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if data for today exists; false otherwise. Returns false on error (and logs).</returns>
    public async Task<bool> DataExistsForTodayAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
            if (await db.IsDataAlreadyIngestedToday(cancellationToken))
            {
                _logger.LogInformation("Data for today already in DB, skipping run. Next run at scheduled time.");
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checking existing data for today failed: {Message}", ex.Message);
            return false;
        }
    }
}
