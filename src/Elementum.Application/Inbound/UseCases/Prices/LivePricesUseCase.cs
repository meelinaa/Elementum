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
    public Task<LiveMarketOverviewDto> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default) =>
        FetchLiveMarketOverviewCoreAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<TradingPriceDto?> GetLiveTradingAnalysisAsync(
        string symbol,
        string currency = DomainConstants.Currencies.Eur,
        CancellationToken cancellationToken = default)
    {
        var normSymbol = symbol.Trim().ToUpperInvariant();
        var normCurrency = Currency.FromCode(
            string.IsNullOrWhiteSpace(currency) ? DomainConstants.Currencies.Eur : currency).Code;

        var quote = await _quotesProvider.GetLiveQuoteAsync(cancellationToken);
        if (!TryExtractMetalQuote(quote, normSymbol, normCurrency, out var metalName, out var livePrice))
            return null;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var summaries = await _repository.GetDailySummariesAsync(yesterday, today, cancellationToken);

        var todayCandle = FindCandle(summaries, normSymbol, normCurrency, today);
        var yesterdayCandle = FindCandle(summaries, normSymbol, normCurrency, yesterday);
        var session = ResolveSession(todayCandle, yesterdayCandle, livePrice);

        var analysis = TradingAnalysisCalculator.Calculate(
            livePrice, session.Open, session.High, session.Low, session.PrevClose);

        return new TradingPriceDto
        {
            Symbol = normSymbol,
            MetalName = metalName,
            Exchange = DefaultExchangeName,
            Currency = normCurrency,
            EntryDate = today,
            ReferenceTimestamp = quote.Timestamp,
            Price = livePrice,
            OpenPrice = session.Open,
            HighPrice = session.High,
            LowPrice = session.Low,
            PrevClosePrice = session.PrevClose,
            Ch = analysis.Ch,
            Chp = analysis.Chp,
            DifferencePrevClose = analysis.DiffPrevClose,
            VolatilityRange = analysis.VolatilityRange,
            VolatilityPercent = analysis.VolatilityPercent,
            Status = analysis.Status,
            ExchangeRateUsdEur = quote.WechselkursUsdEur
        };
    }

    private static bool TryExtractMetalQuote(
        EdelmetalleApiResponse quote,
        string symbol,
        string currency,
        out string name,
        out decimal livePrice)
    {
        bool isEur = string.Equals(currency, DomainConstants.Currencies.Eur, StringComparison.OrdinalIgnoreCase);

        switch (symbol)
        {
            case DomainConstants.Symbols.Gold:
                name = DomainConstants.Names.Gold;
                livePrice = isEur ? quote.GoldEur : quote.GoldUsd;
                return true;
            case DomainConstants.Symbols.Silver:
                name = DomainConstants.Names.Silver;
                livePrice = isEur ? quote.SilberEur : quote.SilberUsd;
                return true;
            case DomainConstants.Symbols.Platinum:
                name = DomainConstants.Names.Platinum;
                livePrice = isEur ? quote.PlatinEur : quote.PlatinUsd;
                return true;
            case DomainConstants.Symbols.Palladium:
                name = DomainConstants.Names.Palladium;
                livePrice = isEur ? quote.PalladiumEur : quote.PalladiumUsd;
                return true;
            default:
                name = string.Empty;
                livePrice = 0m;
                return false;
        }
    }

    private async Task<LiveMarketOverviewDto> FetchLiveMarketOverviewCoreAsync(CancellationToken cancellationToken)
    {
        var quote = await _quotesProvider.GetLiveQuoteAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);
        var summaries = await _repository.GetDailySummariesAsync(yesterday, today, cancellationToken);

        return MapOverview(quote, today, yesterday, summaries);
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
