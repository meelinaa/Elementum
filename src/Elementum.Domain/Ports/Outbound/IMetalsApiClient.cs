using Elementum.Domain.Models;

namespace Elementum.Domain.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Port: External HTTP client abstraction for querying precious metal spot prices.
/// </summary>
public interface IMetalsApiClient
{
    /// <summary>Calls the external API and returns a list of today's prices for configured metals.</summary>
    Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default);
}
