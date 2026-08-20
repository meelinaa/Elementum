using System.ComponentModel.DataAnnotations;
using Elementum.Domain.Enums;

namespace Elementum.Application.Options;

/// <summary>
/// Configuration for the external metals API (api.edelmetalle.de).
/// Fail-fast validation with DataAnnotations without predefined fallback URLs.
/// </summary>
public class MetalsApiOptions
{
    public const string SectionName = "MetalsApi";

    [Required(ErrorMessage = "MetalsApi:BaseUrl is required and must not be empty.")]
    [Url(ErrorMessage = "MetalsApi:BaseUrl must be a valid absolute URL.")]
    public string BaseUrl { get; set; } = null!;

    [Required(ErrorMessage = "MetalsApi:Currency is required.")]
    public string Currency { get; set; } = "USD";

    public List<MetalTypes> Metals { get; set; } = [MetalTypes.Gold, MetalTypes.Silver, MetalTypes.Platinum, MetalTypes.Palladium];
}
