using Elementum.Shared.Enums;
using Elementum.Shared.Objects;
using Elementum_WorkerService.Options;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace Elementum_WorkerService;

public class Worker(ILogger<Worker> logger, IOptions<MetalsApiOptions> apiOptions, IHostEnvironment env) : BackgroundService
{
    private readonly TimeSpan _runTime = new(12, 0, 0); // 12:00
    private bool _isFirstRun = true; // Flag for the initial start

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // DEBUG: Start immediately the first time in development mode
            if (_isFirstRun && env.IsDevelopment())
            {
                logger.LogInformation("Debug mode detected: Start first run immediately...");
                await DoWorkAsync();
                _isFirstRun = false; // Then back to the normal rhythm
            }

            var now = DateTime.Now;
            var nextRun = now.Date.Add(_runTime);

            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            var delay = nextRun - now;
            logger.LogInformation("Next regular run: {NextRun}", nextRun);

            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
            {
                await DoWorkAsync();
            }
        }
    }

    private async Task DoWorkAsync()
    {
        logger.LogInformation("Work is being done...");
        // 1. get Request to the API
        string? result = await GetAPIDataAsync();


        // 2. process the Request
        // 3. save the result to the database
        await Task.Delay(1000);
    }

    private async Task<string?> GetAPIDataAsync()
    {
        var opts = apiOptions.Value;
        if (string.IsNullOrEmpty(opts.ApiKey))
        {
            logger.LogWarning("METALS_API_KEY not set; check .env or environment variables.");
            return null;
        }

        using HttpClient client = new();
        client.DefaultRequestHeaders.Add("x-access-token", opts.ApiKey);
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        foreach (MetalTypes metal in Enum.GetValues(typeof(MetalTypes)))
        {
            string symbol = metal.ToApiSymbol();
            logger.LogInformation("Fetching data for {Metal} ({Symbol})...", metal, symbol);
            DailyPrices? data = await FetchMetalDataAsync(client, opts.BaseUrl, symbol, CurrencyTypes.USD.ToString());
            if (data != null)
                logger.LogInformation("Price for {Metal}: {Price} USD", metal, data.Price);
            else
                logger.LogWarning("Failed to fetch data for {Metal}", metal);
            await Task.Delay(1000);
        }

        return null;
    }

    private async Task<DailyPrices?> FetchMetalDataAsync(HttpClient client, string baseUrl, string metal, string currency)
    {
        try
        {
            //// GoldAPI base URL pattern: .../api/:symbol/:currency (no date = latest)
            //string url = string.IsNullOrEmpty(baseUrl)
            //    ? $"https://www.goldapi.io/api/{metal}/{currency}"
            //    : baseUrl.Replace(":symbol", metal).Replace(":currency", currency).Replace(":date?", "").Trim();
            string url = $"https://www.goldapi.io/api/{metal}/{currency}";
            DailyPrices? result = await client.GetFromJsonAsync<DailyPrices>(url);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Request failed: {Message}", ex.Message);
            return null;
        }
    }
}