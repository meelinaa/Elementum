using Elementum.Shared.Enums;
using Elementum.Shared.Objects;
using Elementum_WorkerService.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Elementum_WorkerService.Services;

/// <summary>
/// Client for the GoldAPI (goldapi.io). Fetches current metal prices (XAU, XAG, etc.) in USD.
/// </summary>
public class MetalsApiClient
{
    private readonly ILogger<MetalsApiClient> _logger;
    private readonly MetalsApiOptions _options;

    /// <summary>
    /// Initializes the client with logging and API options (key, base URL).
    /// </summary>
    public MetalsApiClient(ILogger<MetalsApiClient> logger, IOptions<MetalsApiOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Fetches latest prices for all configured <see cref="MetalTypes"/> (e.g. Gold, Silver) in USD.
    /// Returns an empty list if the API key is not set or requests fail.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of daily price DTOs from the API; may be empty.</returns>
    public async Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<DailyPrices>();
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("METALS_API_KEY not set; check .env or environment variables.");
            return results;
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-access-token", _options.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        foreach (MetalTypes metal in Enum.GetValues(typeof(MetalTypes)))
        {
            var symbol = metal.ToApiSymbol();
            _logger.LogInformation("Fetching data for {Metal} ({Symbol})...", metal, symbol);
            var data = await FetchMetalDataAsync(client, symbol, CurrencyTypes.USD.ToString(), cancellationToken);
            if (data != null)
            {
                _logger.LogInformation("Price for {Metal}: {Price} USD", metal, data.Price);
                results.Add(data);
            }
            else
                _logger.LogWarning("Failed to fetch data for {Metal}", metal);
            await Task.Delay(1000, cancellationToken);
        }

        return results;
    }

    /// <summary>
    /// Performs a single GET request to the GoldAPI for the given metal symbol and currency.
    /// </summary>
    /// <param name="client">Configured HTTP client (with API key header).</param>
    /// <param name="metal">Metal symbol (e.g. XAU, XAG).</param>
    /// <param name="currency">Currency code (e.g. USD).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized daily price, or null on failure.</returns>
    private static async Task<DailyPrices?> FetchMetalDataAsync(HttpClient client, string metal, string currency, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"https://www.goldapi.io/api/{metal}/{currency}";
            return await client.GetFromJsonAsync<DailyPrices>(url, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
