using Elementum.Application.DTOs;
using Elementum.Application.Models;
using Elementum.Domain.Entities;

namespace Elementum.Application.Mapping;

/// <summary>
/// Central mapper: Transforms domain & persistence entities to application-level DTOs,
/// and maps external upstream vendor payloads into domain entities.
/// Uses <see cref="ArgumentNullException.ThrowIfNull"/> for guard clauses.
/// </summary>
public static class PriceHistoryMapper
{
    /// <summary>Maps a <see cref="Metals"/> entity to <see cref="MetalsDto"/>.</summary>
    public static MetalsDto ToDto(this Metals entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new MetalsDto
        {
            Id = entity.Id,
            Symbol = entity.Symbol ?? string.Empty,
            Name = entity.Name ?? string.Empty
        };
    }

    /// <summary>Maps a <see cref="PriceHistory"/> entity to <see cref="PriceHistoryDto"/>.</summary>
    public static PriceHistoryDto ToPriceHistoryDto(this PriceHistory entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PriceHistoryDto
        {
            Id = entity.Id,
            MetalId = entity.MetalId,
            Currency = entity.Currency ?? string.Empty,
            Symbol = entity.Symbol,
            ReferenceTimestamp = entity.ReferenceTimestamp,
            EntryDate = entity.EntryDate,
            Price = entity.Price,
            Chp = entity.Chp,
            Metal = entity.Metal?.ToDto()
        };
    }

    /// <summary>Maps a <see cref="PriceHistory"/> entity to <see cref="TradingPriceDto"/>.</summary>
    public static TradingPriceDto ToTradingPriceDto(this PriceHistory entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new TradingPriceDto
        {
            Id = entity.Id,
            Symbol = entity.Metal?.Symbol ?? entity.Symbol ?? string.Empty,
            MetalName = entity.Metal?.Name ?? string.Empty,
            Exchange = "EDELMETALLE",
            Currency = entity.Currency ?? string.Empty,
            EntryDate = entity.EntryDate,
            ReferenceTimestamp = entity.ReferenceTimestamp == 0 ? null : entity.ReferenceTimestamp,
            Price = entity.Price,
            PrevClosePrice = entity.PrevClosePrice,
            OpenPrice = entity.OpenPrice,
            LowPrice = entity.LowPrice,
            HighPrice = entity.HighPrice,
            Ch = entity.Ch,
            Chp = entity.Chp
        };
    }

    /// <summary>
    /// Maps an incoming <see cref="EdelmetalleApiResponse"/> into a collection of domain <see cref="PriceHistory"/> entities.
    /// </summary>
    public static IReadOnlyList<PriceHistory> ToPriceHistoryEntities(
        this EdelmetalleApiResponse response,
        IReadOnlyDictionary<string, int> metalSymbolToIdMap,
        DateOnly entryDate)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(metalSymbolToIdMap);

        var list = new List<PriceHistory>(8);

        void TryAdd(string symbol, decimal priceUsd, decimal priceEur)
        {
            if (!metalSymbolToIdMap.TryGetValue(symbol, out int metalId))
                return;

            if (priceUsd > 0)
            {
                list.Add(PriceHistory.Create(
                    metalId: metalId,
                    currency: "USD",
                    entryDate: entryDate,
                    price: priceUsd,
                    symbol: $"{symbol}USD",
                    referenceTimestamp: response.Timestamp));
            }

            if (priceEur > 0)
            {
                list.Add(PriceHistory.Create(
                    metalId: metalId,
                    currency: "EUR",
                    entryDate: entryDate,
                    price: priceEur,
                    symbol: $"{symbol}EUR",
                    referenceTimestamp: response.Timestamp));
            }
        }

        TryAdd("XAU", response.GoldUsd, response.GoldEur);
        TryAdd("XAG", response.SilberUsd, response.SilberEur);
        TryAdd("XPT", response.PlatinUsd, response.PlatinEur);
        TryAdd("XPD", response.PalladiumUsd, response.PalladiumEur);

        return list;
    }

    /// <summary>
    /// Maps a collection of incoming <see cref="DailyPrices"/> into domain <see cref="PriceHistory"/> entities.
    /// </summary>
    public static IReadOnlyList<PriceHistory> ToPriceHistoryEntities(
        this IEnumerable<DailyPrices> prices,
        IReadOnlyDictionary<string, int> metalSymbolToIdMap,
        DateOnly entryDate)
    {
        ArgumentNullException.ThrowIfNull(prices);
        ArgumentNullException.ThrowIfNull(metalSymbolToIdMap);

        var list = new List<PriceHistory>();

        foreach (var p in prices)
        {
            if (!metalSymbolToIdMap.TryGetValue(p.Symbol, out int metalId) &&
                !metalSymbolToIdMap.TryGetValue(p.Metal, out metalId))
            {
                continue;
            }

            if (p.Price <= 0)
                continue;

            list.Add(PriceHistory.Create(
                metalId: metalId,
                currency: string.IsNullOrWhiteSpace(p.Currency) ? "USD" : p.Currency,
                entryDate: entryDate,
                price: p.Price,
                symbol: p.Symbol,
                openPrice: p.OpenPrice > 0 ? p.OpenPrice : null,
                highPrice: p.HighPrice > 0 ? p.HighPrice : null,
                lowPrice: p.LowPrice > 0 ? p.LowPrice : null,
                prevClosePrice: p.PrevClosePrice > 0 ? p.PrevClosePrice : null,
                ch: p.Ch,
                chp: p.Chp,
                referenceTimestamp: p.Timestamp));
        }

        return list;
    }
}
