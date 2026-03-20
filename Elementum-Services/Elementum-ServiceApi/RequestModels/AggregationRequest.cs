using System.ComponentModel.DataAnnotations;

namespace Elementum_ServiceApi.RequestModels;

/// <summary>
/// Route parameters for aggregated price history: GET history/{symbol}/aggregated/{aggregation}/{count} (e.g. history/XAU/aggregated/monthly/12).
/// Count is variable: caller passes how many entries to return (e.g. 31 daily, 52 weekly, 12 monthly, 10 yearly).
/// </summary>
public record AggregationRequest : IValidatableObject
{
    [Required(ErrorMessage = "Aggregation is required.")]
    public string Aggregation { get; init; } = string.Empty;

    /// <summary>Maximum number of data points to return (e.g. 31 daily, 52 weekly, 12 monthly, 10 yearly).</summary>
    [Range(0, 500, ErrorMessage = "Count must be between 0 and 500.")]
    public int Count { get; init; }

    /// <summary>Validates that Aggregation is one of: daily, weekly, monthly, yearly.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validIntervals = new[] { "daily", "weekly", "monthly", "yearly" };

        if (!string.IsNullOrWhiteSpace(Aggregation) && !validIntervals.Contains(Aggregation.Trim(), StringComparer.OrdinalIgnoreCase))
            yield return new ValidationResult("Aggregation must be one of: daily, weekly, monthly, yearly.", new[] { nameof(Aggregation) });
    }
}
