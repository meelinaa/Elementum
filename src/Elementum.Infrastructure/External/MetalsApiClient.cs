using System.Net.Http.Json;
using Elementum.Application.Options;
using Elementum.Domain.Enums;
using Elementum.Domain.Models;
using Elementum.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.External;

/// <summary>
/// Secondary / Driven Adapter: Client for GoldAPI (goldapi.io). Fetches current metal prices.
/// </summary>
public class MetalsApiClient : IMetalsApiClient
{
    private readonly ILogger<MetalsApiClient> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetalsApiOptions _options;

    public record ApiStatusResponse(bool Result);

    public MetalsApiClient(IHttpClientFactory httpClientFactory, ILogger<MetalsApiClient> logger, IOptions<MetalsApiOptions> options)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IReadOnlyList<DailyPrices>> GetPricesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<DailyPrices>();
        if (string.IsNullOrEmpty(_options.ApiKey))
        {
            _logger.LogWarning("METALS_API_KEY not set; check .env or environment variables.");
            return results;
        }

        var client = _httpClientFactory.CreateClient("GoldApi");

        if (await ApiIsAvailable(client, cancellationToken))
        {
            foreach (MetalTypes metal in Enum.GetValues(typeof(MetalTypes)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var symbol = metal.ToApiSymbol();
                _logger.LogInformation("Fetching data for {Metal} ({Symbol})...", metal, symbol);
                var data = await FetchMetalDataAsync(client, symbol, CurrencyTypes.USD.ToString(), cancellationToken);
                if (data != null)
                {
                    results.Add(data);
                }
            }
        }
        else
        {
            _logger.LogError("Metals API status check failed or API is unavailable.");
        }

        return results;
    }

    public async Task<bool> ApiIsAvailable(HttpClient client, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri(_options.BaseUrl.TrimEnd('/') + "/status");
            using var response = await client.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return false;

            var status = await response.Content.ReadFromJsonAsync<ApiStatusResponse>(cancellationToken: cancellationToken);
            return status?.Result ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API status check failed.");
            return false;
        }
    }

    public async Task<DailyPrices?> FetchMetalDataAsync(HttpClient client, string metalSymbol, string currency, CancellationToken cancellationToken = default)
    {
        try
        {
            var uri = new Uri($"{_options.BaseUrl.TrimEnd('/')}/{metalSymbol}/{currency}");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("x-access-token", _options.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch data for {MetalSymbol}: {StatusCode}", metalSymbol, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<DailyPrices>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception fetching metal data for {MetalSymbol}", metalSymbol);
            return null;
        }
    }
}
