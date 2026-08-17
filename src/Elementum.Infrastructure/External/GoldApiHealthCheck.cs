using System.Net.Http.Json;
using Elementum.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.External;

/// <summary>
/// Health check that verifies whether the GoldAPI is reachable.
/// </summary>
public class GoldApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<MetalsApiOptions> _options;
    private readonly ILogger<GoldApiHealthCheck> _logger;

    private record ApiStatusResponse(bool Result);

    public GoldApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<MetalsApiOptions> options,
        ILogger<GoldApiHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;
        if (string.IsNullOrWhiteSpace(opt.BaseUrl))
        {
            return HealthCheckResult.Degraded("GoldAPI BaseUrl is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GoldApi");
            var uri = new Uri(opt.BaseUrl.TrimEnd('/') + "/status");
            using var response = await client.GetAsync(uri, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Degraded($"GoldAPI returned status {response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<ApiStatusResponse>(cancellationToken: cancellationToken);
            if (body?.Result == true)
            {
                return HealthCheckResult.Healthy("GoldAPI is available.");
            }

            return HealthCheckResult.Degraded("GoldAPI status returned result=false.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GoldAPI health check failed.");
            return HealthCheckResult.Unhealthy("GoldAPI is unreachable.", ex);
        }
    }
}
