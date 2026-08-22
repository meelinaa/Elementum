using Elementum.Application.Options;
using Elementum.Infrastructure.Outbound.External.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elementum.Infrastructure.Outbound.External;

/// <summary>
/// Health check that verifies whether the GoldAPI is reachable.
/// Uses <see cref="GoldApiHealthCheckLogMessages"/> for zero-allocation logging.
/// </summary>
public class GoldApiHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<MetalsApiOptions> _options;
    private readonly ILogger<GoldApiHealthCheck> _logger;

    public GoldApiHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<MetalsApiOptions> options,
        ILogger<GoldApiHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var opt = _options.Value;
        if (string.IsNullOrWhiteSpace(opt.BaseUrl))
        {
            return HealthCheckResult.Degraded("Metals API BaseUrl is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GoldApi");
            using var response = await client.GetAsync(opt.BaseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Edelmetalle API is available.");
            }

            return HealthCheckResult.Degraded($"Edelmetalle API returned status {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            GoldApiHealthCheckLogMessages.HealthCheckFailed(_logger, ex);
            return HealthCheckResult.Unhealthy("Edelmetalle API is unreachable.", ex);
        }
    }
}
