using System.Net.Http.Json;
using Elementum.Application.Options;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.External;

/// <summary>
/// Secondary / Driven Adapter: Client for api.edelmetalle.de.
/// Fetches precious metal spot prices (Gold, Silver, Platinum, Palladium in USD and EUR) in a single request.
/// </summary>
public class MetalsApiClient : IMetalsApiClient
{
    private readonly ILogger<MetalsApiClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetalsApiOptions _options;

    public MetalsApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<MetalsApiClient> logger,
        IOptions<MetalsApiOptions> options)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<EdelmetalleApiResponse?> GetEdelmetallePricesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var client = _httpClientFactory.CreateClient("GoldApi");
        var url = _options.BaseUrl;

        try
        {
            _logger.LogInformation("Fetching precious metal prices from {Url}...", url);
            var response = await client.GetFromJsonAsync<EdelmetalleApiResponse>(url, cancellationToken);
            if (response == null)
            {
                _logger.LogWarning("Empty response received from {Url}", url);
                return null;
            }

            _logger.LogInformation(
                "Successfully fetched prices: Gold USD={GoldUsd}, EUR={GoldEur}, Silber USD={SilberUsd}, EUR={SilberEur}, Rate={Rate}",
                response.GoldUsd, response.GoldEur, response.SilberUsd, response.SilberEur, response.WechselkursUsdEur);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch precious metal prices from {Url}", url);
            throw;
        }
    }

    public async Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetEdelmetallePricesAsync(cancellationToken);
        if (response == null)
            return Array.Empty<DailyPrices>();

        var results = new List<DailyPrices>
        {
            // Gold
            new() { Metal = "Gold", Currency = "USD", Price = response.GoldUsd, Symbol = "XAUUSD", Timestamp = response.Timestamp },
            new() { Metal = "Gold", Currency = "EUR", Price = response.GoldEur, Symbol = "XAUEUR", Timestamp = response.Timestamp },

            // Silver / Silber
            new() { Metal = "Silver", Currency = "USD", Price = response.SilberUsd, Symbol = "XAGUSD", Timestamp = response.Timestamp },
            new() { Metal = "Silver", Currency = "EUR", Price = response.SilberEur, Symbol = "XAGEUR", Timestamp = response.Timestamp },

            // Platinum / Platin
            new() { Metal = "Platinum", Currency = "USD", Price = response.PlatinUsd, Symbol = "XPTUSD", Timestamp = response.Timestamp },
            new() { Metal = "Platinum", Currency = "EUR", Price = response.PlatinEur, Symbol = "XPTEUR", Timestamp = response.Timestamp },

            // Palladium
            new() { Metal = "Palladium", Currency = "USD", Price = response.PalladiumUsd, Symbol = "XPDUSD", Timestamp = response.Timestamp },
            new() { Metal = "Palladium", Currency = "EUR", Price = response.PalladiumEur, Symbol = "XPDEUR", Timestamp = response.Timestamp }
        };

        return results;
    }
}
