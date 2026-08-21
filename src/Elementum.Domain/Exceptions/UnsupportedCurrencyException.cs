namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when a currency code is not supported by the domain model.
/// </summary>
public sealed class UnsupportedCurrencyException : DomainException
{
    private UnsupportedCurrencyException(string code)
        : base($"Currency '{code}' is not supported. Supported currencies are USD and EUR.")
    {
        Code = code;
    }

    public string Code { get; }

    public static UnsupportedCurrencyException ForCode(string code) => new(code);
}
