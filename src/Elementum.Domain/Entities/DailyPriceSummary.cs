using Elementum.Domain.Exceptions;

namespace Elementum.Domain.Entities;

/// <summary>
/// Domain Entity & Aggregate Root representing the daily consolidated price candle (Min/Max/Open/Close at 22:00).
/// Retained permanently for candlestick charts and long-term trend analytics.
/// <see cref="UpdatedAtUtc"/> is the optimistic concurrency token, configured in Infrastructure Fluent API.
/// </summary>
public class DailyPriceSummary
{
    public int Id { get; private set; }
    public int MetalId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public DateOnly EntryDate { get; private set; }
    public decimal OpenPrice { get; private set; }
    public decimal HighPrice { get; private set; }
    public decimal LowPrice { get; private set; }
    public decimal ClosePrice { get; private set; }
    public decimal? ExchangeRateUsdEur { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>
    /// UTC timestamp of the last update. Persistence maps this as an optimistic concurrency token.
    /// </summary>
    public DateTime UpdatedAtUtc { get; private set; }

    public Metals? Metal { get; private set; }

    private DailyPriceSummary() { }

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
        decimal? exchangeRateUsdEur = null,
        Metals? metal = null)
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
            UpdatedAtUtc = now,
            Metal = metal
        };
    }

    /// <summary>
    /// Replaces the full OHLC candle (e.g. after a daily rollup of all ticks).
    /// </summary>
    public void UpdateCandle(
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal closePrice,
        decimal? exchangeRateUsdEur = null)
    {
        ValidateInvariants(MetalId, Currency, openPrice, highPrice, lowPrice, closePrice);

        OpenPrice = openPrice;
        HighPrice = highPrice;
        LowPrice = lowPrice;
        ClosePrice = closePrice;

        if (exchangeRateUsdEur.HasValue)
            ExchangeRateUsdEur = exchangeRateUsdEur.Value;

        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the candle with a new price tick during the trading day.
    /// </summary>
    public void ApplyPriceTick(decimal tickPrice, decimal? exchangeRateUsdEur = null, bool isClosePrice = false)
    {
        DomainThrowHelper.ThrowIfNegativeOrZero(tickPrice, nameof(tickPrice));

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
        DomainThrowHelper.ThrowIfNegativeOrZero(metalId, nameof(metalId));
        DomainThrowHelper.ThrowIfNullOrWhiteSpace(currency, nameof(currency));

        ValueObjects.Currency.FromCode(currency);

        if (openPrice <= 0 || highPrice <= 0 || lowPrice <= 0 || closePrice <= 0)
            throw InvalidPriceException.AllPricesMustBePositive();

        if (lowPrice > highPrice)
            throw PriceRangeInvalidException.For(lowPrice, highPrice);
    }
}
