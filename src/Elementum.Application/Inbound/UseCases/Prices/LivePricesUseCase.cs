using Elementum.Application.DTOs;
using Elementum.Application.Models;
using Elementum.Application.Services;
using Elementum.Domain.Constants;
using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Elementum.Domain.Services;
using Elementum.Domain.ValueObjects;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Interactor: live quotes plus one server-side candle query; trading metrics come from the domain service.
/// </summary>
public class LivePricesUseCase : ILivePricesUseCase
{
    private const string DefaultExchangeName = "EDELMETALLE";

    private readonly ILiveQuotesProvider _quotesProvider;
    private readonly IPriceHistoryReadRepository _repository;

    public LivePricesUseCase(
        ILiveQuotesProvider quotesProvider,
        IPriceHistoryReadRepository repository)
    {
        ArgumentNullException.ThrowIfNull(quotesProvider);
        ArgumentNullException.ThrowIfNull(repository);

        _quotesProvider = quotesProvider;
        _repository = repository;
    }

    /// <inheritdoc />
    public async Task<LiveMarketOverviewDto> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        var quote = await _quotesProvider.GetLiveQuoteAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var summaries = await _repository.GetDailySummariesAsync(yesterday, today, cancellationToken);

        return MapOverview(quote, today, yesterday, summaries);
    }

    /// <inheritdoc />
    public async Task<TradingPriceDto?> GetLiveTradingAnalysisAsync(
        string symbol,
        string currency = DomainConstants.Currencies.Eur,
        CancellationToken cancellationToken = default)
    {
        var normSymbol = symbol.Trim().ToUpperInvariant();
        var normCurrency = Currency.FromCode(
            string.IsNullOrWhiteSpace(currency) ? DomainConstants.Currencies.Eur : currency).Code;

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

        var analysis = TradingAnalysisCalculator.Calculate(currentPrice, openPrice, highPrice, lowPrice, prevClose);

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
            Ch = analysis.Ch,
            Chp = analysis.Chp,
            DifferencePrevClose = analysis.DiffPrevClose,
            VolatilityRange = analysis.VolatilityRange,
            VolatilityPercent = analysis.VolatilityPercent,
            Status = analysis.Status,
            ExchangeRateUsdEur = overview.ExchangeRateUsdEur
        };
    }

    private static LiveMarketOverviewDto MapOverview(
        EdelmetalleApiResponse quote,
        DateOnly today,
        DateOnly yesterday,
        IReadOnlyList<DailyPriceSummary> summaries)
    {
        var metals = new (string Symbol, string Name, decimal Usd, decimal Eur)[]
        {
            (DomainConstants.Symbols.Gold, DomainConstants.Names.Gold, quote.GoldUsd, quote.GoldEur),
            (DomainConstants.Symbols.Silver, DomainConstants.Names.Silver, quote.SilberUsd, quote.SilberEur),
            (DomainConstants.Symbols.Platinum, DomainConstants.Names.Platinum, quote.PlatinUsd, quote.PlatinEur),
            (DomainConstants.Symbols.Palladium, DomainConstants.Names.Palladium, quote.PalladiumUsd, quote.PalladiumEur)
        };

        var items = metals.Select(m => MapMetalItem(m.Symbol, m.Name, m.Usd, m.Eur, today, yesterday, summaries)).ToList();

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

    private static LiveMetalPriceDto MapMetalItem(
        string symbol,
        string name,
        decimal liveUsd,
        decimal liveEur,
        DateOnly today,
        DateOnly yesterday,
        IReadOnlyList<DailyPriceSummary> summaries)
    {
        var usd = ResolveSession(
            FindCandle(summaries, symbol, DomainConstants.Currencies.Usd, today),
            FindCandle(summaries, symbol, DomainConstants.Currencies.Usd, yesterday),
            liveUsd);
        var eur = ResolveSession(
            FindCandle(summaries, symbol, DomainConstants.Currencies.Eur, today),
            FindCandle(summaries, symbol, DomainConstants.Currencies.Eur, yesterday),
            liveEur);

        var usdAnalysis = TradingAnalysisCalculator.Calculate(liveUsd, usd.Open, usd.High, usd.Low, usd.PrevClose);
        var eurAnalysis = TradingAnalysisCalculator.Calculate(liveEur, eur.Open, eur.High, eur.Low, eur.PrevClose);

        return new LiveMetalPriceDto
        {
            Symbol = symbol,
            Name = name,
            PriceUsd = liveUsd,
            PriceEur = liveEur,
            OpenPriceUsd = usd.Open,
            OpenPriceEur = eur.Open,
            ChpUsd = usdAnalysis.Chp,
            ChpEur = eurAnalysis.Chp,
            HighPriceUsd = usd.High,
            HighPriceEur = eur.High,
            LowPriceUsd = usd.Low,
            LowPriceEur = eur.Low,
            PrevCloseUsd = usd.PrevClose,
            PrevCloseEur = eur.PrevClose
        };
    }

    private static DailyPriceSummary? FindCandle(
        IReadOnlyList<DailyPriceSummary> summaries,
        string symbol,
        string currency,
        DateOnly date) =>
        summaries.FirstOrDefault(s =>
            s.EntryDate == date &&
            string.Equals(s.Currency, currency, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.Metal?.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

    private static (decimal Open, decimal High, decimal Low, decimal PrevClose) ResolveSession(
        DailyPriceSummary? today,
        DailyPriceSummary? yesterday,
        decimal livePrice)
    {
        decimal open = today?.OpenPrice ?? livePrice;
        decimal high = today != null ? Math.Max(today.HighPrice, livePrice) : livePrice;
        decimal low = today != null ? Math.Min(today.LowPrice, livePrice) : livePrice;
        decimal prevClose = yesterday?.ClosePrice ?? open;
        return (open, high, low, prevClose);
    }
}
