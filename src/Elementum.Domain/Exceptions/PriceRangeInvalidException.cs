namespace Elementum.Domain.Exceptions;

/// <summary>
/// Thrown when LowPrice exceeds HighPrice.
/// </summary>
public sealed class PriceRangeInvalidException : ArgumentException
{
    private PriceRangeInvalidException(decimal lowPrice, decimal highPrice)
        : base($"Low price ({lowPrice}) cannot exceed High price ({highPrice}).")
    {
    }

    public static PriceRangeInvalidException For(decimal lowPrice, decimal highPrice) => new(lowPrice, highPrice);
}
