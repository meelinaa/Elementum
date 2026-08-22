using Elementum.Application.Models;

namespace Elementum.Application.Ports.Outbound;

/// <summary>
/// Secondary / Driven Outbound Port: Contract for fetching precious metals market quotes from remote vendor APIs.
/// </summary>
public interface IMetalsApiClient
{
    /// <summary>Fetches raw daily prices for all metals from GoldAPI or equivalent remote provider.</summary>
    Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches current market quotes for Gold, Silver, Platinum, and Palladium in USD & EUR from Edelmetalle API.</summary>
    Task<EdelmetalleApiResponse?> GetEdelmetallePricesAsync(CancellationToken cancellationToken = default);
}
