namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is attempted across mismatched currency units.
/// </summary>
public sealed class CurrencyMismatchException : InvalidOperationException
{
    private CurrencyMismatchException(string codeA, string codeB)
        : base($"Cannot perform arithmetic/comparison on mismatching currencies: '{codeA}' and '{codeB}'.")
    {
    }

    public static CurrencyMismatchException For(string codeA, string codeB) => new(codeA, codeB);
}
