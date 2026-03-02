using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Elementum_WorkerService.Options;

namespace Elementum_WorkerService.HealthChecks;

/// <summary>
/// Health check that calls GoldAPI status endpoint (GET api/status) and verifies response <c>{"result": true}</c>.
/// </summary>
public sealed class GoldApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MetalsApiOptions _options;

    public GoldApiHealthCheck(IHttpClientFactory httpClientFactory, IOptions<MetalsApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            return HealthCheckResult.Degraded("GoldAPI health check skipped: no API key configured.");

        try
        {
            var client = _httpClientFactory.CreateClient("GoldApi");
            var path = string.IsNullOrWhiteSpace(_options.StatusUrl) ? "api/status" : _options.StatusUrl.Trim();
            var response = await client.GetAsync(path, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return HealthCheckResult.Unhealthy($"GoldAPI status returned {response.StatusCode}.");

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var resultProp) && resultProp.ValueKind == JsonValueKind.True)
                return HealthCheckResult.Healthy("GoldAPI is reachable.");
            return HealthCheckResult.Unhealthy("GoldAPI status did not return result: true.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("GoldAPI health check failed.", ex);
        }
    }
}
