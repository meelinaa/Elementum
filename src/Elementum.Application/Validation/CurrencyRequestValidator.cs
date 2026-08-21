using Elementum.Application.Requests;
using Elementum.Domain.Constants;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates the currency query parameter at the HTTP boundary (USD or EUR only), analog to <see cref="SymbolRequestValidator"/>.
/// </summary>
public class CurrencyRequestValidator : AbstractValidator<CurrencyRequest>
{
    public CurrencyRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Currency), () =>
        {
            RuleFor(x => x.Currency)
                .Must(BeSupportedCurrency)
                .WithMessage("Currency must be USD or EUR.");
        });
    }

    private static bool BeSupportedCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return false;

        var code = currency.Trim().ToUpperInvariant();
        return code is DomainConstants.Currencies.Usd or DomainConstants.Currencies.Eur;
    }
}
