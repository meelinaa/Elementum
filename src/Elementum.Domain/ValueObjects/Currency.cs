namespace Elementum.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing supported currencies (EUR and USD only).
/// </summary>
public readonly record struct Currency : IEquatable<Currency>
{
    public string Code { get; }
    public int DecimalPlaces { get; }
    public string Symbol { get; }

    public static readonly Currency USD = new("USD", 2, "$");
    public static readonly Currency EUR = new("EUR", 2, "€");

    private static readonly Dictionary<string, Currency> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = USD,
        ["EUR"] = EUR
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
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be null or whitespace.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();

        if (SupportedCurrencies.TryGetValue(normalized, out var currency))
            return currency;

        throw new ArgumentException($"Currency '{code}' is not supported. Supported currencies are USD and EUR.", nameof(code));
    }

    public override string ToString() => Code;

    public static implicit operator string(Currency currency) => currency.Code;
}
