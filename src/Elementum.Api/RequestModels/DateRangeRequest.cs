using System.ComponentModel.DataAnnotations;

namespace Elementum.Api.RequestModels;

/// <summary>
/// Request model for date range parameters (e.g. from route or query).
/// Validates format (yyyy-MM-dd) and that start date is not after end date.
/// </summary>
public record DateRangeRequest : IValidatableObject
{
    [Required(ErrorMessage = "FirstDate is required.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "FirstDate must be in format yyyy-MM-dd.")]
    public string FirstDate { get; init; } = string.Empty;

    [Required(ErrorMessage = "LastDate is required.")]
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "LastDate must be in format yyyy-MM-dd.")]
    public string LastDate { get; init; } = string.Empty;

    /// <summary>Validates that FirstDate and LastDate are parseable and that start is not after end.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(FirstDate) || string.IsNullOrWhiteSpace(LastDate))
            yield break;

        if (!DateOnly.TryParse(FirstDate, out var start) || !DateOnly.TryParse(LastDate, out var end))
            yield break;

        if (start > end)
        {
            yield return new ValidationResult(
                "Start date must not be after end date.",
                new[] { nameof(FirstDate), nameof(LastDate) });
        }
    }
}
