using Elementum.Shared.Objects;

namespace Elementum.WorkerService.Abstractions;

/// <summary>
/// Fetches current metal prices from the external API (e.g. GoldAPI).
/// </summary>
public interface IMetalsApiClient
{
    /// <summary>
    /// Fetches latest prices for all configured metals in the configured currency.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of daily price DTOs; may be empty on failure or missing config.</returns>
    Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default);
}
