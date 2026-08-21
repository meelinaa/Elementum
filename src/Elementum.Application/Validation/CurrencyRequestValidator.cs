using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Syntactic checks for the currency query parameter. Supported ISO codes are enforced in the domain.
/// </summary>
public class CurrencyRequestValidator : AbstractValidator<CurrencyRequest>
{
    public CurrencyRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Currency), () =>
        {
            RuleFor(x => x.Currency)
                .Must(BeThreeLetterCode)
                .WithMessage("Currency must be a 3-letter ISO code (USD or EUR).");
        });
    }

    private static bool BeThreeLetterCode(string? currency) =>
        currency is not null && currency.Trim().Length == 3 && currency.Trim().All(char.IsAsciiLetter);
}
