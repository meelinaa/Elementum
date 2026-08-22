using Elementum.Domain.Exceptions;

namespace Elementum.Domain.ValueObjects;

/// <summary>
/// Immutable Domain Value Object representing an OHLC (Open, High, Low, Close) candlestick.
/// Encapsulates domain invariants (High >= Low, non-negative prices) and candle aggregation logic.
/// </summary>
public readonly record struct OhlcCandle
{
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }

    public OhlcCandle(decimal open, decimal high, decimal low, decimal close)
    {
        if (open < 0) throw InvalidPriceException.MustBePositive(nameof(open), open);
        if (high < 0) throw InvalidPriceException.MustBePositive(nameof(high), high);
        if (low < 0) throw InvalidPriceException.MustBePositive(nameof(low), low);
        if (close < 0) throw InvalidPriceException.MustBePositive(nameof(close), close);
        if (high < low) throw PriceRangeInvalidException.For(low, high);

        Open = open;
        High = high;
        Low = low;
        Close = close;
    }

    /// <summary>Creates a candle with all 4 prices initialized from a single price tick.</summary>
    public static OhlcCandle FromSinglePrice(decimal price) => new(price, price, price, price);

    /// <summary>Applies a tick price to the candle, expanding High/Low boundaries.</summary>
    public OhlcCandle ApplyTick(decimal tickPrice, bool isCloseTick = false)
    {
        if (tickPrice < 0)
            throw InvalidPriceException.MustBePositive(nameof(tickPrice), tickPrice);

        var open = Open == 0 ? tickPrice : Open;
        var high = Math.Max(High, tickPrice);
        var low = Low == 0 ? tickPrice : Math.Min(Low, tickPrice);
        var close = isCloseTick ? tickPrice : (Close == 0 ? tickPrice : Close);

        return new OhlcCandle(open, high, low, close);
    }

    /// <summary>Absolute spread between High and Low.</summary>
    public decimal VolatilityRange => High - Low;

    /// <summary>Percentage spread relative to Low.</summary>
    public decimal VolatilityPercent => Low == 0 ? 0 : Math.Round(((High - Low) / Low) * 100m, 2);
}
