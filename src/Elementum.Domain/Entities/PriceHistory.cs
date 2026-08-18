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
    public string ReferenceTimestamp { get; set; } = string.Empty;
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
        string referenceTimestamp = "")
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
    /// Updates quote prices while enforcing domain invariants.
    /// </summary>
    public void UpdatePrices(
        decimal price,
        decimal? highPrice = null,
        decimal? lowPrice = null,
        decimal? openPrice = null,
        decimal? prevClosePrice = null,
        decimal? ch = null,
        decimal? chp = null,
        string? referenceTimestamp = null,
        string? symbol = null)
    {
        ValidateInvariants(MetalId, Currency, price, highPrice, lowPrice);

        Price = price;
        HighPrice = highPrice ?? HighPrice;
        LowPrice = lowPrice ?? LowPrice;
        OpenPrice = openPrice ?? OpenPrice;
        PrevClosePrice = prevClosePrice ?? PrevClosePrice;
        Ch = ch ?? Ch;
        Chp = chp ?? Chp;

        if (referenceTimestamp != null) ReferenceTimestamp = referenceTimestamp;
        if (symbol != null) Symbol = symbol;
    }

    private static void ValidateInvariants(
        int metalId,
        string currency,
        decimal price,
        decimal? highPrice,
        decimal? lowPrice)
    {
        if (metalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(metalId), "MetalId must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency must not be null or whitespace.", nameof(currency));

        // Enforce supported currencies (EUR, USD)
        ValueObjects.Currency.FromCode(currency);

        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be strictly positive (> 0).");

        if (lowPrice.HasValue && highPrice.HasValue && lowPrice > highPrice)
            throw new ArgumentException($"Low price ({lowPrice}) cannot be greater than High price ({highPrice}).");
    }
}
