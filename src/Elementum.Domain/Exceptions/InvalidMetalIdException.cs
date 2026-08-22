namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when a metal ID invariant is violated (e.g. non-positive ID).
/// </summary>
public sealed class InvalidMetalIdException : ArgumentOutOfRangeException
{
    private InvalidMetalIdException(int actualValue)
        : base("metalId", actualValue, "MetalId must be greater than zero.")
    {
    }

    public static InvalidMetalIdException MustBePositive(int actualValue) => new(actualValue);
}
