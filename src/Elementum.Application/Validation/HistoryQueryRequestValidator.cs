using Elementum.Application.Requests;
using FluentValidation;
using FluentValidation.Results;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates the combined history query: currency (USD/EUR), ISO date window, and skip/take bounds.
/// </summary>
public class HistoryQueryRequestValidator : AbstractValidator<HistoryQueryRequest>
{
    public HistoryQueryRequestValidator()
    {
        RuleFor(x => x).Custom((request, context) =>
        {
            AddFailures(context, new CurrencyRequestValidator().Validate(new CurrencyRequest(request.Currency)));
            AddFailures(context, new DateRangeRequestValidator().Validate(new DateRangeRequest(request.From, request.To)));
            AddFailures(context, new HistoryPageRequestValidator().Validate(new HistoryPageRequest
            {
                Skip = request.Skip,
                Take = request.Take
            }));
        });
    }

    private static void AddFailures(ValidationContext<HistoryQueryRequest> context, ValidationResult result)
    {
        foreach (var error in result.Errors)
            context.AddFailure(error.PropertyName, error.ErrorMessage);
    }
}
