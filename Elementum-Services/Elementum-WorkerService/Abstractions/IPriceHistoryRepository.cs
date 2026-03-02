using Elementum.Shared.Objects;

namespace Elementum_WorkerService.Abstractions;

/// <summary>
/// Persists API price data into the price_history table.
/// </summary>
public interface IPriceHistoryRepository
{
    /// <summary>
    /// Saves the given API prices to the database (resolving metal_id from metals table).
    /// </summary>
    /// <param name="prices">Prices from the API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default);
}
