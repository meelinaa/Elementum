using System.Globalization;
using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates date range requests to ensure ISO yyyy-MM-dd format and Start &lt;= End &lt;= Today.
/// </summary>
public class DateRangeRequestValidator : AbstractValidator<DateRangeRequest>
{
    private const string DateFormat = "yyyy-MM-dd";

    public DateRangeRequestValidator()
    {
        RuleFor(x => x.FirstDate)
            .NotEmpty().WithMessage("FirstDate (start date) is required.")
            .Must(BeValidIsoDate).WithMessage("FirstDate must be a valid date in ISO format yyyy-MM-dd.");

        RuleFor(x => x.LastDate)
            .NotEmpty().WithMessage("LastDate (end date) is required.")
            .Must(BeValidIsoDate).WithMessage("LastDate must be a valid date in ISO format yyyy-MM-dd.");

        RuleFor(x => x)
            .Must(HaveValidChronologicalRange)
            .WithMessage("FirstDate cannot be after LastDate.")
            .When(x => BeValidIsoDate(x.FirstDate) && BeValidIsoDate(x.LastDate));

        RuleFor(x => x.LastDate)
            .Must(NotBeInTheFuture)
            .WithMessage("LastDate cannot be in the future.")
            .When(x => BeValidIsoDate(x.LastDate));
    }

    private static bool BeValidIsoDate(string dateStr)
    {
        return DateOnly.TryParseExact(dateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool HaveValidChronologicalRange(DateRangeRequest req)
    {
        if (DateOnly.TryParseExact(req.FirstDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) &&
            DateOnly.TryParseExact(req.LastDate, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
        {
            return start <= end;
        }
        return false;
    }

    private static bool NotBeInTheFuture(string lastDateStr)
    {
        if (DateOnly.TryParseExact(lastDateStr, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastDate))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            return lastDate <= today;
        }
        return false;
    }
}
