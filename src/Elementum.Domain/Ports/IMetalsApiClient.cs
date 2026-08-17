using Elementum.Domain.Models;

namespace Elementum.Domain.Ports;

/// <summary>
/// Secondary / Driven Port: Abstraction for fetching metals price data from external provider (e.g. GoldAPI).
/// </summary>
public interface IMetalsApiClient
{
    /// <summary>Fetches current metal prices for all configured metals.</summary>
    Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default);
}
