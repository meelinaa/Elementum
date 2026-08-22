namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when a price invariant is violated (e.g. non-positive price).
/// </summary>
public sealed class InvalidPriceException : ArgumentOutOfRangeException
{
    private InvalidPriceException(string paramName, decimal actualValue, string message)
        : base(paramName, actualValue, message)
    {
    }

    public static InvalidPriceException MustBePositive(string paramName, decimal actualValue) =>
        new(paramName, actualValue, "Price must be strictly positive (> 0).");

    public static InvalidPriceException AllPricesMustBePositive() =>
        new("prices", 0m, "All prices (Open, High, Low, Close) must be strictly positive (> 0).");
}
