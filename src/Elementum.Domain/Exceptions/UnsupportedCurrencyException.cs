namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when a currency code is not supported by the domain model.
/// </summary>
public sealed class UnsupportedCurrencyException : ArgumentException
{
    private UnsupportedCurrencyException(string code)
        : base($"Currency '{code}' is not supported. Supported currencies are USD and EUR.", nameof(code))
    {
    }

    public static UnsupportedCurrencyException ForCode(string code) => new(code);
}
