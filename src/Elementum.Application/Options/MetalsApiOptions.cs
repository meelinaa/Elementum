using Elementum.Domain.Enums;

namespace Elementum.Application.Options;

/// <summary>
/// Configuration for the external metals API (api.edelmetalle.de).
/// </summary>
public class MetalsApiOptions
{
    public const string SectionName = "MetalsApi";

    public string BaseUrl { get; set; } = "https://api.edelmetalle.de/public.json";
    public string ApiKey { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public List<MetalTypes> Metals { get; set; } = new() { MetalTypes.Gold, MetalTypes.Silver, MetalTypes.Platinum, MetalTypes.Palladium };
}
