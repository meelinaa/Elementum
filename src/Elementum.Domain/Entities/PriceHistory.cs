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
    public string Exchange { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string ReferenceTimestamp { get; set; } = string.Empty;
    public DateOnly EntryDate { get; set; }
    public decimal Price { get; set; }
    public decimal? PrevClosePrice { get; set; }
    public decimal? OpenPrice { get; set; }
    public decimal? LowPrice { get; set; }
    public decimal? HighPrice { get; set; }
    public string OpenTime { get; set; } = string.Empty;
    public decimal? Ch { get; set; }
    public decimal? Chp { get; set; }
    public decimal? Ask { get; set; }
    public decimal? Bid { get; set; }
    public decimal? PriceGram24k { get; set; }
    public decimal? PriceGram22k { get; set; }
    public decimal? PriceGram21k { get; set; }
    public decimal? PriceGram20k { get; set; }
    public decimal? PriceGram18k { get; set; }
    public decimal? PriceGram16k { get; set; }
    public decimal? PriceGram14k { get; set; }
    public decimal? PriceGram10k { get; set; }

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
        string exchange = "",
        decimal? openPrice = null,
        decimal? highPrice = null,
        decimal? lowPrice = null,
        decimal? prevClosePrice = null,
        decimal? ask = null,
        decimal? bid = null,
        decimal? ch = null,
        decimal? chp = null,
        decimal? priceGram24k = null,
        decimal? priceGram22k = null,
        decimal? priceGram21k = null,
        decimal? priceGram20k = null,
        decimal? priceGram18k = null,
        decimal? priceGram16k = null,
        decimal? priceGram14k = null,
        decimal? priceGram10k = null,
        string referenceTimestamp = "",
        string openTime = "")
    {
        ValidateInvariants(metalId, currency, price, highPrice, lowPrice, ask, bid);

        return new PriceHistory
        {
            MetalId = metalId,
            Currency = currency.Trim().ToUpperInvariant(),
            EntryDate = entryDate,
            Price = price,
            Symbol = symbol,
            Exchange = exchange,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PrevClosePrice = prevClosePrice,
            Ask = ask,
            Bid = bid,
            Ch = ch,
            Chp = chp,
            PriceGram24k = priceGram24k,
            PriceGram22k = priceGram22k,
            PriceGram21k = priceGram21k,
            PriceGram20k = priceGram20k,
            PriceGram18k = priceGram18k,
            PriceGram16k = priceGram16k,
            PriceGram14k = priceGram14k,
            PriceGram10k = priceGram10k,
            ReferenceTimestamp = referenceTimestamp,
            OpenTime = openTime
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
        decimal? ask = null,
        decimal? bid = null,
        decimal? ch = null,
        decimal? chp = null,
        decimal? priceGram24k = null,
        decimal? priceGram22k = null,
        decimal? priceGram21k = null,
        decimal? priceGram20k = null,
        decimal? priceGram18k = null,
        decimal? priceGram16k = null,
        decimal? priceGram14k = null,
        decimal? priceGram10k = null,
        string? referenceTimestamp = null,
        string? openTime = null,
        string? exchange = null,
        string? symbol = null)
    {
        ValidateInvariants(MetalId, Currency, price, highPrice, lowPrice, ask, bid);

        Price = price;
        HighPrice = highPrice ?? HighPrice;
        LowPrice = lowPrice ?? LowPrice;
        OpenPrice = openPrice ?? OpenPrice;
        PrevClosePrice = prevClosePrice ?? PrevClosePrice;
        Ask = ask ?? Ask;
        Bid = bid ?? Bid;
        Ch = ch ?? Ch;
        Chp = chp ?? Chp;
        PriceGram24k = priceGram24k ?? PriceGram24k;
        PriceGram22k = priceGram22k ?? PriceGram22k;
        PriceGram21k = priceGram21k ?? PriceGram21k;
        PriceGram20k = priceGram20k ?? PriceGram20k;
        PriceGram18k = priceGram18k ?? PriceGram18k;
        PriceGram16k = priceGram16k ?? PriceGram16k;
        PriceGram14k = priceGram14k ?? PriceGram14k;
        PriceGram10k = priceGram10k ?? PriceGram10k;

        if (referenceTimestamp != null) ReferenceTimestamp = referenceTimestamp;
        if (openTime != null) OpenTime = openTime;
        if (exchange != null) Exchange = exchange;
        if (symbol != null) Symbol = symbol;
    }

    private static void ValidateInvariants(
        int metalId,
        string currency,
        decimal price,
        decimal? highPrice,
        decimal? lowPrice,
        decimal? ask,
        decimal? bid)
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

        if (bid.HasValue && ask.HasValue && bid > ask)
            throw new ArgumentException($"Bid price ({bid}) cannot be greater than Ask price ({ask}).");
    }
}
