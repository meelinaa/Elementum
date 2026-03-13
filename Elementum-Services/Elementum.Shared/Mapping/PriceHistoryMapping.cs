using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;

namespace Elementum.Shared.Mapping;

/// <summary>
/// Maps entity types to API DTOs so responses expose only the required fields.
/// </summary>
public static class PriceHistoryMapping
{
    public static MetalsDto ToDto(this Metals entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return new MetalsDto
        {
            Id = entity.Id,
            Symbol = entity.Symbol ?? string.Empty,
            Name = entity.Name ?? string.Empty
        };
    }

    public static PriceHistoryDto ToPriceHistoryDto(this PriceHistory entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return new PriceHistoryDto
        {
            Id = entity.Id,
            MetalId = entity.MetalId,
            Currency = entity.Currency ?? string.Empty,
            Exchange = entity.Exchange,
            Symbol = entity.Symbol,
            ReferenceTimestamp = entity.ReferenceTimestamp,
            EntryDate = entity.EntryDate,
            Price = entity.Price,
            Chp = entity.Chp,
            Metal = entity.Metal?.ToDto()
        };
    }

    public static PriceSummaryDto ToPriceSummaryDto(this PriceHistory entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return new PriceSummaryDto
        {
            Symbol = entity.Metal?.Symbol ?? entity.Symbol ?? string.Empty,
            Name = entity.Metal?.Name ?? string.Empty,
            Exchange = entity.Exchange,
            Price = entity.Price,
            Chp = entity.Chp,
            EntryDate = entity.EntryDate
        };
    }

    public static KaratPricesDto ToKaratPricesDto(this PriceHistory entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return new KaratPricesDto
        {
            Symbol = entity.Metal?.Symbol ?? entity.Symbol ?? string.Empty,
            MetalName = entity.Metal?.Name ?? string.Empty,
            EntryDate = entity.EntryDate,
            Currency = entity.Currency ?? string.Empty,
            PriceGram24k = entity.PriceGram24k,
            PriceGram22k = entity.PriceGram22k,
            PriceGram21k = entity.PriceGram21k,
            PriceGram20k = entity.PriceGram20k,
            PriceGram18k = entity.PriceGram18k,
            PriceGram16k = entity.PriceGram16k,
            PriceGram14k = entity.PriceGram14k,
            PriceGram10k = entity.PriceGram10k
        };
    }

    public static TradingPriceDto ToTradingPriceDto(this PriceHistory entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        return new TradingPriceDto
        {
            Id = entity.Id,
            Symbol = entity.Metal?.Symbol ?? entity.Symbol ?? string.Empty,
            MetalName = entity.Metal?.Name ?? string.Empty,
            Exchange = entity.Exchange,
            Currency = entity.Currency ?? string.Empty,
            EntryDate = entity.EntryDate,
            ReferenceTimestamp = entity.ReferenceTimestamp,
            OpenTime = entity.OpenTime,
            Price = entity.Price,
            PrevClosePrice = entity.PrevClosePrice,
            OpenPrice = entity.OpenPrice,
            LowPrice = entity.LowPrice,
            HighPrice = entity.HighPrice,
            Ch = entity.Ch,
            Chp = entity.Chp,
            Ask = entity.Ask,
            Bid = entity.Bid
        };
    }
}
