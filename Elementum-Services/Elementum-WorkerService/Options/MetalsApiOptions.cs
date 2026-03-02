namespace Elementum_WorkerService.Options;

/// <summary>
/// Configuration for the GoldAPI (goldapi.io): API key and optional base/status URLs. Bound from config (e.g. METALS_API_KEY in .env).
/// </summary>
public class MetalsApiOptions
{
    public const string SectionName = "MetalsApi";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
    public string RequestStatsUrl { get; set; } = string.Empty;
}
