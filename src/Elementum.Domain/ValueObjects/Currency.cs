using Elementum.Domain.Constants;
using Elementum.Domain.Exceptions;

namespace Elementum.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing supported currencies (EUR and USD only).
/// </summary>
public readonly record struct Currency : IEquatable<Currency>
{
    public const int DefaultDecimalPlaces = 2;
    public const string UsdSymbol = "$";
    public const string EurSymbol = "€";

    public string Code { get; }
    public int DecimalPlaces { get; }
    public string Symbol { get; }

    public static readonly Currency USD = new(DomainConstants.Currencies.Usd, DefaultDecimalPlaces, UsdSymbol);
    public static readonly Currency EUR = new(DomainConstants.Currencies.Eur, DefaultDecimalPlaces, EurSymbol);

    private static readonly Dictionary<string, Currency> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        [DomainConstants.Currencies.Usd] = USD,
        [DomainConstants.Currencies.Eur] = EUR
    };

    private Currency(string code, int decimalPlaces, string symbol)
    {
        Code = code;
        DecimalPlaces = decimalPlaces;
        Symbol = symbol;
    }

    /// <summary>
    /// Creates or resolves a <see cref="Currency"/> from a currency code (only EUR and USD are supported).
    /// </summary>
    public static Currency FromCode(string code)
    {
        DomainThrowHelper.ThrowIfNullOrWhiteSpace(code, nameof(code));

        var normalized = code.Trim().ToUpperInvariant();

        if (SupportedCurrencies.TryGetValue(normalized, out var currency))
            return currency;

        throw UnsupportedCurrencyException.ForCode(code);
    }

    public override string ToString() => Code;

    public static implicit operator string(Currency currency) => currency.Code;
}
