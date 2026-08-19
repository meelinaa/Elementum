using Elementum.Domain.Exceptions;

namespace Elementum.Domain.Entities;

/// <summary>
/// Domain Entity & Aggregate Root representing precious metal price history and daily quotes.
/// Encapsulates invariants to prevent invalid price states.
/// </summary>
public class PriceHistory
{
    public int Id { get; set; }
    public int MetalId { get; set; }
    public string Currency { get; set; } = "USD";
    public string Symbol { get; set; } = string.Empty;
    public long ReferenceTimestamp { get; set; }
    public DateOnly EntryDate { get; set; }
    public decimal Price { get; set; }
    public decimal? PrevClosePrice { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public decimal? Ch { get; set; }
    public decimal? Chp { get; set; }

    public Metals? Metal { get; set; }

    public PriceHistory() { }

    /// <summary>
    /// Factory method to create a validated <see cref="PriceHistory"/> instance.
    /// </summary>
    public static PriceHistory Create(
        int metalId,
        string currency,
        DateOnly entryDate,
        decimal price,
        string symbol = "",
        decimal? openPrice = null,
        decimal? highPrice = null,
        decimal? lowPrice = null,
        decimal? prevClosePrice = null,
        decimal? ch = null,
        decimal? chp = null,
        long referenceTimestamp = 0)
    {
        ValidateInvariants(metalId, currency, price, highPrice, lowPrice);

        return new PriceHistory
        {
            MetalId = metalId,
            Currency = currency.Trim().ToUpperInvariant(),
            EntryDate = entryDate,
            Price = price,
            Symbol = symbol,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PrevClosePrice = prevClosePrice,
            Ch = ch,
            Chp = chp,
            ReferenceTimestamp = referenceTimestamp
        };
    }

    /// <summary>
    /// Updates prices on an existing <see cref="PriceHistory"/> entity.
    /// </summary>
    public void UpdatePrices(
        decimal price,
        decimal? highPrice = null,
        decimal? lowPrice = null,
        decimal? openPrice = null,
        decimal? prevClosePrice = null,
        decimal? ch = null,
        decimal? chp = null,
        long referenceTimestamp = 0,
        string symbol = "")
    {
        ValidateInvariants(MetalId, Currency, price, highPrice, lowPrice);

        Price = price;
        HighPrice = highPrice;
        LowPrice = lowPrice;
        OpenPrice = openPrice;
        PrevClosePrice = prevClosePrice;
        Ch = ch;
        Chp = chp;

        if (referenceTimestamp > 0)
            ReferenceTimestamp = referenceTimestamp;
        if (!string.IsNullOrEmpty(symbol))
            Symbol = symbol;
    }

    private static void ValidateInvariants(
        int metalId,
        string currency,
        decimal price,
        decimal? highPrice,
        decimal? lowPrice)
    {
        DomainThrowHelper.ThrowIfNegativeOrZero(metalId, nameof(metalId));
        DomainThrowHelper.ThrowIfNullOrWhiteSpace(currency, nameof(currency));

        // Enforce valid domain currency (USD or EUR)
        ValueObjects.Currency.FromCode(currency);

        DomainThrowHelper.ThrowIfNegativeOrZero(price, nameof(price));

        if (highPrice.HasValue && lowPrice.HasValue && highPrice.Value < lowPrice.Value)
            throw PriceRangeInvalidException.For(lowPrice.Value, highPrice.Value);
    }
}
