namespace Elementum.Domain.Entities;

/// <summary>
/// Domain Entity & Aggregate Root representing the daily consolidated price candle (Min/Max/Open/Close at 22:00).
/// Retained permanently for candlestick charts and long-term trend analytics.
/// </summary>
public class DailyPriceSummary
{
    public int Id { get; set; }
    public int MetalId { get; set; }
    public string Currency { get; set; } = "USD";
    public DateOnly EntryDate { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal? ExchangeRateUsdEur { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Metals? Metal { get; set; }

    public DailyPriceSummary() { }

    /// <summary>
    /// Factory method to create or initialize a <see cref="DailyPriceSummary"/> with invariant validation.
    /// </summary>
    public static DailyPriceSummary Create(
        int metalId,
        string currency,
        DateOnly entryDate,
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal closePrice,
        decimal? exchangeRateUsdEur = null)
    {
        ValidateInvariants(metalId, currency, openPrice, highPrice, lowPrice, closePrice);

        var now = DateTime.UtcNow;
        return new DailyPriceSummary
        {
            MetalId = metalId,
            Currency = currency.Trim().ToUpperInvariant(),
            EntryDate = entryDate,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            ClosePrice = closePrice,
            ExchangeRateUsdEur = exchangeRateUsdEur,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    /// <summary>
    /// Updates the candle with a new price tick during the trading day.
    /// </summary>
    public void ApplyPriceTick(decimal tickPrice, decimal? exchangeRateUsdEur = null, bool isClosePrice = false)
    {
        if (tickPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(tickPrice), "Tick price must be positive.");

        if (OpenPrice <= 0)
            OpenPrice = tickPrice;

        if (tickPrice > HighPrice || HighPrice == 0)
            HighPrice = tickPrice;

        if (tickPrice < LowPrice || LowPrice == 0)
            LowPrice = tickPrice;

        if (isClosePrice || ClosePrice == 0)
            ClosePrice = tickPrice;

        if (exchangeRateUsdEur.HasValue)
            ExchangeRateUsdEur = exchangeRateUsdEur.Value;

        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidateInvariants(
        int metalId,
        string currency,
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal closePrice)
    {
        if (metalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(metalId), "MetalId must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be null or whitespace.", nameof(currency));

        // Enforce supported currencies (EUR, USD)
        ValueObjects.Currency.FromCode(currency);

        if (openPrice <= 0 || highPrice <= 0 || lowPrice <= 0 || closePrice <= 0)
            throw new ArgumentOutOfRangeException("All prices (Open, High, Low, Close) must be strictly positive (> 0).");

        if (lowPrice > highPrice)
            throw new ArgumentException($"Low price ({lowPrice}) cannot exceed High price ({highPrice}).");
    }
}
