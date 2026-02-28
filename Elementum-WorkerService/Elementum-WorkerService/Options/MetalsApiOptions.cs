namespace Elementum_WorkerService.Options;

public class MetalsApiOptions
{
    public const string SectionName = "MetalsApi";

    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
    public string RequestStatsUrl { get; set; } = string.Empty;
}
