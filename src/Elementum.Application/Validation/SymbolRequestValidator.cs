using System.Text.RegularExpressions;
using Elementum.Application.Requests;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Validates precious metal symbol requests.
/// </summary>
public partial class SymbolRequestValidator : AbstractValidator<SymbolRequest>
{
    private static readonly string[] AllowedStandardSymbols = ["XAU", "XAG", "XPT", "XPD", "GOLD", "SILVER", "PLATINUM", "PALLADIUM"];

    public SymbolRequestValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty().WithMessage("Symbol is required and cannot be empty.")
            .MinimumLength(2).WithMessage("Symbol must be at least 2 characters long.")
            .MaximumLength(15).WithMessage("Symbol cannot exceed 15 characters.")
            .Must(BeValidSymbolFormat).WithMessage("Symbol must be a valid alphanumeric identifier (e.g. XAU, XAG, XPT, XPD).");
    }

    private static bool BeValidSymbolFormat(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;

        return SymbolRegex().IsMatch(symbol.Trim());
    }

    [GeneratedRegex(@"^[A-Za-z0-9_:\-]+$")]
    private static partial Regex SymbolRegex();
}
