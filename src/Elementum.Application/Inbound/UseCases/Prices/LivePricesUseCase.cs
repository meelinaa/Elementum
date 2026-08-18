using Elementum.Application.DTOs;
using Elementum.Application.Services;
using Elementum.Domain.Constants;
using Elementum.Domain.Ports.Outbound;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Interactor: Implements live price querying and daily trading aggregations.
/// Coordinates live quotes, repository history, and trading metrics calculations without magic literals.
/// </summary>
public class LivePricesUseCase(
    ILiveQuotesProvider quotesProvider,
    IPriceHistoryRepository repository) : ILivePricesUseCase
{
    private const string DefaultExchangeName = "EDELMETALLE";

    private readonly ILiveQuotesProvider _quotesProvider = quotesProvider ?? throw new ArgumentNullException(nameof(quotesProvider));
    private readonly IPriceHistoryRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    /// <inheritdoc />
    public async Task<LiveMarketOverviewDto> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        var quote = await _quotesProvider.GetLiveQuoteAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var metals = new (string Symbol, string Name, decimal Usd, decimal Eur)[]
        {
            (DomainConstants.Symbols.Gold, DomainConstants.Names.Gold, quote.GoldUsd, quote.GoldEur),
            (DomainConstants.Symbols.Silver, DomainConstants.Names.Silver, quote.SilberUsd, quote.SilberEur),
            (DomainConstants.Symbols.Platinum, DomainConstants.Names.Platinum, quote.PlatinUsd, quote.PlatinEur),
            (DomainConstants.Symbols.Palladium, DomainConstants.Names.Palladium, quote.PalladiumUsd, quote.PalladiumEur)
        };

        var items = new List<LiveMetalPriceDto>();

        foreach (var m in metals)
        {
            var historyTodayUsd = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(m.Symbol, today, today)
                .Where(p => p.Currency == DomainConstants.Currencies.Usd)
                .OrderBy(p => p.Id)
                .ToList();

            var historyTodayEur = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(m.Symbol, today, today)
                .Where(p => p.Currency == DomainConstants.Currencies.Eur)
                .OrderBy(p => p.Id)
                .ToList();

            decimal openUsd = historyTodayUsd.Count > 0 ? (historyTodayUsd[0].OpenPrice ?? historyTodayUsd[0].Price) : m.Usd;
            decimal openEur = historyTodayEur.Count > 0 ? (historyTodayEur[0].OpenPrice ?? historyTodayEur[0].Price) : m.Eur;

            decimal chpUsd = openUsd > 0
                ? Math.Round(((m.Usd - openUsd) / openUsd) * DomainConstants.Trading.PercentageMultiplier, DomainConstants.Trading.DefaultPrecisionDecimals)
                : 0m;
            decimal chpEur = openEur > 0
                ? Math.Round(((m.Eur - openEur) / openEur) * DomainConstants.Trading.PercentageMultiplier, DomainConstants.Trading.DefaultPrecisionDecimals)
                : 0m;

            decimal highUsd = historyTodayUsd.Count > 0 ? Math.Max(historyTodayUsd.Max(p => p.HighPrice ?? p.Price), m.Usd) : m.Usd;
            decimal lowUsd = historyTodayUsd.Count > 0 ? Math.Min(historyTodayUsd.Min(p => p.LowPrice ?? p.Price), m.Usd) : m.Usd;

            decimal highEur = historyTodayEur.Count > 0 ? Math.Max(historyTodayEur.Max(p => p.HighPrice ?? p.Price), m.Eur) : m.Eur;
            decimal lowEur = historyTodayEur.Count > 0 ? Math.Min(historyTodayEur.Min(p => p.LowPrice ?? p.Price), m.Eur) : m.Eur;

            // Get yesterday close from daily candles
            var yesterdaySummariesUsd = await _repository.GetDailySummariesAsync(m.Symbol, DomainConstants.Currencies.Usd, yesterday, yesterday, cancellationToken);
            decimal? prevCloseUsd = yesterdaySummariesUsd.FirstOrDefault()?.ClosePrice;

            var yesterdaySummariesEur = await _repository.GetDailySummariesAsync(m.Symbol, DomainConstants.Currencies.Eur, yesterday, yesterday, cancellationToken);
            decimal? prevCloseEur = yesterdaySummariesEur.FirstOrDefault()?.ClosePrice;

            items.Add(new LiveMetalPriceDto
            {
                Symbol = m.Symbol,
                Name = m.Name,
                PriceUsd = m.Usd,
                PriceEur = m.Eur,
                OpenPriceUsd = openUsd,
                OpenPriceEur = openEur,
                ChpUsd = chpUsd,
                ChpEur = chpEur,
                HighPriceUsd = highUsd,
                HighPriceEur = highEur,
                LowPriceUsd = lowUsd,
                LowPriceEur = lowEur,
                PrevCloseUsd = prevCloseUsd ?? openUsd,
                PrevCloseEur = prevCloseEur ?? openEur
            });
        }

        DateTime localTime = quote.Timestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(quote.Timestamp).ToLocalTime().DateTime
            : DateTime.Now;

        return new LiveMarketOverviewDto
        {
            Items = items,
            ExchangeRateUsdEur = quote.WechselkursUsdEur,
            Timestamp = quote.Timestamp,
            LastUpdatedAtLocal = localTime
        };
    }

    /// <inheritdoc />
    public async Task<TradingPriceDto?> GetLiveTradingAnalysisAsync(string symbol, string currency = DomainConstants.Currencies.Eur, CancellationToken cancellationToken = default)
    {
        var normSymbol = symbol.Trim().ToUpperInvariant();
        var normCurrency = currency.Trim().ToUpperInvariant();
        if (normCurrency != DomainConstants.Currencies.Usd && normCurrency != DomainConstants.Currencies.Eur)
            normCurrency = DomainConstants.Currencies.Eur;

        var overview = await GetLiveMarketOverviewAsync(cancellationToken);
        var metal = overview.Items.FirstOrDefault(i => i.Symbol.Equals(normSymbol, StringComparison.OrdinalIgnoreCase));
        if (metal == null)
            return null;

        bool isEur = normCurrency == DomainConstants.Currencies.Eur;
        decimal currentPrice = isEur ? metal.PriceEur : metal.PriceUsd;
        decimal openPrice = (isEur ? metal.OpenPriceEur : metal.OpenPriceUsd) ?? currentPrice;
        decimal highPrice = (isEur ? metal.HighPriceEur : metal.HighPriceUsd) ?? currentPrice;
        decimal lowPrice = (isEur ? metal.LowPriceEur : metal.LowPriceUsd) ?? currentPrice;
        decimal prevClose = (isEur ? metal.PrevCloseEur : metal.PrevCloseUsd) ?? openPrice;

        var (ch, chp, diffPrevClose, status, volatilityRange, volatilityPct) =
            TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

        return new TradingPriceDto
        {
            Symbol = metal.Symbol,
            MetalName = metal.Name,
            Exchange = DefaultExchangeName,
            Currency = normCurrency,
            EntryDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ReferenceTimestamp = overview.Timestamp,
            Price = currentPrice,
            OpenPrice = openPrice,
            HighPrice = highPrice,
            LowPrice = lowPrice,
            PrevClosePrice = prevClose,
            Ch = ch,
            Chp = chp,
            DifferencePrevClose = diffPrevClose,
            VolatilityRange = volatilityRange,
            VolatilityPercent = volatilityPct,
            Status = status,
            ExchangeRateUsdEur = overview.ExchangeRateUsdEur
        };
    }
}
