using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates offset pagination. Oversized <c>take</c> is not a 400 — the use case clamps it to <see cref="HistoryQueryLimits.MaxTake"/>.
/// </summary>
public class HistoryPageRequestValidator : AbstractValidator<HistoryPageRequest>
{
    public HistoryPageRequestValidator()
    {
        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Skip cannot be negative.");

        When(x => x.Take.HasValue, () =>
        {
            RuleFor(x => x.Take)
                .Must(take => take is > 0)
                .WithMessage("Take must be at least 1 when specified.");
        });
    }
}
