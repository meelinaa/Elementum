using System.Globalization;
using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates optional history date bounds: ISO yyyy-MM-dd when present, From &lt;= To, To not in the future.
/// Both omitted is valid (use-case default of the last 30 days).
/// </summary>
public class DateRangeRequestValidator : AbstractValidator<DateRangeRequest>
{
    public DateRangeRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.From), () =>
        {
            RuleFor(x => x.From)
                .Must(BeValidIsoDate!)
                .WithMessage("From must be a valid date in ISO format yyyy-MM-dd.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.To), () =>
        {
            RuleFor(x => x.To)
                .Must(BeValidIsoDate!)
                .WithMessage("To must be a valid date in ISO format yyyy-MM-dd.");
        });

        RuleFor(x => x)
            .Must(HaveValidChronologicalRange)
            .WithMessage("From cannot be after To.")
            .When(x => BeValidIsoDate(x.From) && BeValidIsoDate(x.To));

        RuleFor(x => x.To)
            .Must(NotBeInTheFuture!)
            .WithMessage("To cannot be in the future.")
            .When(x => BeValidIsoDate(x.To));

        RuleFor(x => x.From)
            .Must(NotBeInTheFuture!)
            .WithMessage("From cannot be in the future.")
            .When(x => BeValidIsoDate(x.From) && string.IsNullOrWhiteSpace(x.To));
    }

    private static bool BeValidIsoDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr))
            return false;

        return DateOnly.TryParseExact(
            dateStr.Trim(),
            HistoryQueryLimits.DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    private static bool HaveValidChronologicalRange(DateRangeRequest req)
    {
        if (DateOnly.TryParseExact(req.From!.Trim(), HistoryQueryLimits.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateOnly.TryParseExact(req.To!.Trim(), HistoryQueryLimits.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return start <= end;
        }

        return false;
    }

    private static bool NotBeInTheFuture(string dateStr)
    {
        if (DateOnly.TryParseExact(
                dateStr.Trim(),
                HistoryQueryLimits.DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return date <= today;
        }

        return false;
    }
}
