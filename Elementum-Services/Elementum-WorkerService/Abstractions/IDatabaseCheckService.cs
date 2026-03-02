namespace Elementum_WorkerService.Abstractions;

/// <summary>
/// Precondition checks for the ingestion job: database availability and whether data for today already exists.
/// </summary>
public interface IDatabaseCheckService
{
    /// <summary>
    /// Verifies that the database is reachable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the connection succeeded; false otherwise.</returns>
    Task<bool> IsDatabaseAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether at least one row exists in price_history for today's date (UTC).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if data for today exists; false otherwise.</returns>
    Task<bool> DataExistsForTodayAsync(CancellationToken cancellationToken = default);
}
