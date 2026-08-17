using System.Globalization;
using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates aggregation query requests.
/// </summary>
public class AggregationRequestValidator : AbstractValidator<AggregationRequest>
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly string[] ValidIntervals = ["daily", "weekly", "monthly", "yearly", "d", "w", "m", "y"];

    public AggregationRequestValidator()
    {
        RuleFor(x => x.FirstDate)
            .Must(BeValidIsoDateOrEmpty)
            .WithMessage("FirstDate must be empty or in ISO format yyyy-MM-dd.");

        RuleFor(x => x.LastDate)
            .Must(BeValidIsoDateOrEmpty)
            .WithMessage("LastDate must be empty or in ISO format yyyy-MM-dd.");

        RuleFor(x => x.Interval)
            .Must(BeValidInterval)
            .WithMessage("Interval must be one of: daily, weekly, monthly, yearly.")
            .When(x => !string.IsNullOrWhiteSpace(x.Interval));
    }

    private static bool BeValidIsoDateOrEmpty(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return true;

        return DateOnly.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool BeValidInterval(string? interval)
    {
        if (string.IsNullOrWhiteSpace(interval))
            return true;

        return ValidIntervals.Contains(interval.Trim().ToLowerInvariant());
    }
}
