namespace Elementum.Domain.Exceptions;

/// <summary>
/// Centralized guard helper providing static factory methods and throw helpers to eliminate inline exception allocation noise.
/// </summary>
public static class DomainThrowHelper
{
    public static void ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw DomainValidationException.NullOrWhitespace(paramName);
    }

    public static void ThrowIfNegativeOrZero(decimal value, string paramName)
    {
        if (value <= 0)
            throw InvalidPriceException.MustBePositive(paramName, value);
    }

    public static void ThrowIfNegativeOrZero(int value, string paramName)
    {
        if (value <= 0)
            throw InvalidMetalIdException.MustBePositive(value);
    }

    public static void ThrowIfZero(decimal value, string message)
    {
        if (value == 0)
            throw DomainArithmeticException.DivideByZero(message);
    }
}
