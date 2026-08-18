using Elementum.Application.DTOs;
using Elementum.Domain.Entities;

namespace Elementum.Application.Mapping;

/// <summary>
/// Central mapper: Transforms domain & persistence entities to application-level DTOs,
/// ensuring the API contract is decoupled from internal database schema and entity details.
/// </summary>
public static class PriceHistoryMapper
{
    /// <summary>Maps a <see cref="Metals"/> entity to <see cref="MetalsDto"/>.</summary>
    public static MetalsDto ToDto(this Metals entity)
    {
        return entity == null
            ? throw new ArgumentNullException(nameof(entity))
            : new MetalsDto
            {
                Id = entity.Id,
                Symbol = entity.Symbol ?? string.Empty,
                Name = entity.Name ?? string.Empty
            };
    }

    /// <summary>Maps a <see cref="PriceHistory"/> entity to <see cref="PriceHistoryDto"/>.</summary>
    public static PriceHistoryDto ToPriceHistoryDto(this PriceHistory entity)
    {
        return entity == null
            ? throw new ArgumentNullException(nameof(entity))
            : new PriceHistoryDto
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

    /// <summary>Maps a <see cref="DailyPriceSummary"/> entity to <see cref="DailyPriceSummaryDto"/>.</summary>
    public static DailyPriceSummaryDto ToDailyPriceSummaryDto(this DailyPriceSummary summary, string? symbol = null, string? metalName = null)
    {
        return summary == null
            ? throw new ArgumentNullException(nameof(summary))
            : new DailyPriceSummaryDto
            {
                Id = summary.Id,
                MetalId = summary.MetalId,
                Symbol = symbol ?? summary.Metal?.Symbol ?? string.Empty,
                MetalName = metalName ?? summary.Metal?.Name ?? string.Empty,
                Currency = summary.Currency,
                EntryDate = summary.EntryDate,
                OpenPrice = summary.OpenPrice,
                HighPrice = summary.HighPrice,
                LowPrice = summary.LowPrice,
                ClosePrice = summary.ClosePrice,
                ExchangeRateUsdEur = summary.ExchangeRateUsdEur
            };
    }

    /// <summary>Maps a <see cref="PriceHistory"/> entity to <see cref="KaratPricesDto"/>.</summary>
    public static KaratPricesDto ToKaratPricesDto(this PriceHistory entity)
    {
        return entity == null
            ? throw new ArgumentNullException(nameof(entity))
            : new KaratPricesDto
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

    /// <summary>Maps a <see cref="PriceHistory"/> entity to <see cref="TradingPriceDto"/>.</summary>
    public static TradingPriceDto ToTradingPriceDto(this PriceHistory entity)
    {
        long? refTimestamp = long.TryParse(entity?.ReferenceTimestamp, out var rt) ? rt : null;
        long? openTime = long.TryParse(entity?.OpenTime, out var ot) ? ot : null;

        return entity == null
            ? throw new ArgumentNullException(nameof(entity))
            : new TradingPriceDto
            {
                Id = entity.Id,
                Symbol = entity.Metal?.Symbol ?? entity.Symbol ?? string.Empty,
                MetalName = entity.Metal?.Name ?? string.Empty,
                Exchange = entity.Exchange,
                Currency = entity.Currency ?? string.Empty,
                EntryDate = entity.EntryDate,
                ReferenceTimestamp = refTimestamp,
                OpenTime = openTime,
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
