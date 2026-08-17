namespace Elementum_WorkerService.Options;

/// <summary>
/// Configuration for the GoldAPI (goldapi.io): API key and optional base/status URLs. Bound from config (e.g. METALS_API_KEY in .env).
/// </summary>
/// <remarks>
/// Uses <c>set</c> accessors so <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> and <c>Configure&lt;T&gt;</c> can mutate after binding.
/// </remarks>
public record MetalsApiOptions
{
    public const string SectionName = "MetalsApi";

    /// <summary>Base address for the GoldAPI HttpClient (e.g. https://www.goldapi.io/).</summary>
    public string BaseAddress { get; set; } = "https://www.goldapi.io/";

    /// <summary>API key for GoldAPI (x-access-token header). Required for price requests.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Template URL for price requests (e.g. api/:symbol/:currency).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>URL for status check (e.g. api/status).</summary>
    public string StatusUrl { get; set; } = string.Empty;

    /// <summary>URL for request stats (optional).</summary>
    public string RequestStatsUrl { get; set; } = string.Empty;
}
