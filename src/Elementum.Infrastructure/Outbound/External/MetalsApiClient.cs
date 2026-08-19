using System.Text.Json;
using Elementum.Application.Options;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Elementum.Infrastructure.External.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.External;

/// <summary>
/// Secondary / Driven Adapter: Client for api.edelmetalle.de.
/// Fetches precious metal spot prices (Gold, Silver, Platinum, Palladium in USD and EUR) in a single request.
/// Employs streaming HTTP deserialization (ResponseHeadersRead + ReadAsStreamAsync) for zero string buffering.
/// Uses source-generated <see cref="MetalsApiClientLogMessages"/> for zero-allocation logging.
/// </summary>
public class MetalsApiClient : IMetalsApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILogger<MetalsApiClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetalsApiOptions _options;

    public MetalsApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<MetalsApiClient> logger,
        IOptions<MetalsApiOptions> options)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Value);

        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<EdelmetalleApiResponse?> GetEdelmetallePricesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var client = _httpClientFactory.CreateClient("GoldApi");
        var url = _options.BaseUrl;

        try
        {
            MetalsApiClientLogMessages.StreamingPricesFromUrl(_logger, url);

            // Stream directly from HTTP response socket to minimize memory footprint
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<EdelmetalleApiResponse>(stream, JsonOptions, cancellationToken);

            if (result == null)
            {
                MetalsApiClientLogMessages.EmptyPayloadReceived(_logger, url);
                return null;
            }

            MetalsApiClientLogMessages.PricesFetchedSuccessfully(
                _logger,
                result.GoldUsd,
                result.GoldEur,
                result.SilberUsd,
                result.SilberEur,
                result.WechselkursUsdEur);

            return result;
        }
        catch (Exception ex)
        {
            MetalsApiClientLogMessages.FailedToFetchPrices(_logger, url, ex);
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
