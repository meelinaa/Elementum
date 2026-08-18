using Elementum.Application.DTOs;
using Elementum.Domain.Models;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Interactor: Implements 5-minute cached live price querying and daily trading aggregations.
/// </summary>
public class LivePricesUseCase : ILivePricesUseCase
{
    private const string CacheKey = "Edelmetalle_LiveQuotes";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IMetalsApiClient _apiClient;
    private readonly IPriceHistoryRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<LivePricesUseCase> _logger;

    public LivePricesUseCase(
        IMetalsApiClient apiClient,
        IPriceHistoryRepository repository,
        IMemoryCache cache,
        ILogger<LivePricesUseCase> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LiveMarketOverviewDto> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default)
    {
        var quote = await GetOrFetchLiveQuoteAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var metals = new (string Symbol, string Name, decimal Usd, decimal Eur)[]
        {
            ("XAU", "Gold", quote.GoldUsd, quote.GoldEur),
            ("XAG", "Silver", quote.SilberUsd, quote.SilberEur),
            ("XPT", "Platinum", quote.PlatinUsd, quote.PlatinEur),
            ("XPD", "Palladium", quote.PalladiumUsd, quote.PalladiumEur)
        };

        var items = new List<LiveMetalPriceDto>();

        foreach (var m in metals)
        {
            var historyTodayUsd = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(m.Symbol, today, today)
                .Where(p => p.Currency == "USD")
                .OrderBy(p => p.Id)
                .ToList();

            var historyTodayEur = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(m.Symbol, today, today)
                .Where(p => p.Currency == "EUR")
                .OrderBy(p => p.Id)
                .ToList();

            decimal openUsd = historyTodayUsd.Count > 0 ? (historyTodayUsd[0].OpenPrice ?? historyTodayUsd[0].Price) : m.Usd;
            decimal openEur = historyTodayEur.Count > 0 ? (historyTodayEur[0].OpenPrice ?? historyTodayEur[0].Price) : m.Eur;

            decimal chpUsd = openUsd > 0 ? Math.Round(((m.Usd - openUsd) / openUsd) * 100m, 2) : 0m;
            decimal chpEur = openEur > 0 ? Math.Round(((m.Eur - openEur) / openEur) * 100m, 2) : 0m;

            decimal highUsd = historyTodayUsd.Count > 0 ? Math.Max(historyTodayUsd.Max(p => p.HighPrice ?? p.Price), m.Usd) : m.Usd;
            decimal lowUsd = historyTodayUsd.Count > 0 ? Math.Min(historyTodayUsd.Min(p => p.LowPrice ?? p.Price), m.Usd) : m.Usd;

            decimal highEur = historyTodayEur.Count > 0 ? Math.Max(historyTodayEur.Max(p => p.HighPrice ?? p.Price), m.Eur) : m.Eur;
            decimal lowEur = historyTodayEur.Count > 0 ? Math.Min(historyTodayEur.Min(p => p.LowPrice ?? p.Price), m.Eur) : m.Eur;

            // Get yesterday close from daily candles
            var yesterdaySummariesUsd = await _repository.GetDailySummariesAsync(m.Symbol, "USD", yesterday, yesterday, cancellationToken);
            decimal? prevCloseUsd = yesterdaySummariesUsd.FirstOrDefault()?.ClosePrice;

            var yesterdaySummariesEur = await _repository.GetDailySummariesAsync(m.Symbol, "EUR", yesterday, yesterday, cancellationToken);
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

    public async Task<TradingPriceDto?> GetLiveTradingAnalysisAsync(string symbol, string currency = "EUR", CancellationToken cancellationToken = default)
    {
        var normSymbol = symbol.Trim().ToUpperInvariant();
        var normCurrency = currency.Trim().ToUpperInvariant();
        if (normCurrency != "USD" && normCurrency != "EUR")
            normCurrency = "EUR";

        var overview = await GetLiveMarketOverviewAsync(cancellationToken);
        var metal = overview.Items.FirstOrDefault(i => i.Symbol.Equals(normSymbol, StringComparison.OrdinalIgnoreCase));
        if (metal == null)
            return null;

        bool isEur = normCurrency == "EUR";
        decimal currentPrice = isEur ? metal.PriceEur : metal.PriceUsd;
        decimal openPrice = (isEur ? metal.OpenPriceEur : metal.OpenPriceUsd) ?? currentPrice;
        decimal highPrice = (isEur ? metal.HighPriceEur : metal.HighPriceUsd) ?? currentPrice;
        decimal lowPrice = (isEur ? metal.LowPriceEur : metal.LowPriceUsd) ?? currentPrice;
        decimal prevClose = (isEur ? metal.PrevCloseEur : metal.PrevCloseUsd) ?? openPrice;

        decimal ch = currentPrice - openPrice;
        decimal chp = openPrice > 0 ? Math.Round((ch / openPrice) * 100m, 2) : 0m;
        decimal diffPrevClose = currentPrice - prevClose;

        string status = diffPrevClose >= 0 ? "BULLISH ▲" : "BEARISH ▼";
        decimal volatilityRange = highPrice - lowPrice;
        decimal volatilityPct = lowPrice > 0 ? Math.Round((volatilityRange / lowPrice) * 100m, 2) : 0m;

        return new TradingPriceDto
        {
            Symbol = metal.Symbol,
            MetalName = metal.Name,
            Exchange = "EDELMETALLE",
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

    private async Task<EdelmetalleApiResponse> GetOrFetchLiveQuoteAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out EdelmetalleApiResponse? cached) && cached != null)
        {
            return cached;
        }

        try
        {
            var quote = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);
            if (quote != null)
            {
                _cache.Set(CacheKey, quote, CacheDuration);
                return quote;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch live quotes from api.edelmetalle.de; using fallback or defaults.");
        }

        // Fallback default quote if API unreachable
        return new EdelmetalleApiResponse
        {
            GoldUsd = 4410.60m,
            GoldEur = 3803.80m,
            SilberUsd = 65.71m,
            SilberEur = 56.68m,
            PlatinUsd = 1773.50m,
            PlatinEur = 1529.59m,
            PalladiumUsd = 1334.00m,
            PalladiumEur = 1150.53m,
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            WechselkursUsdEur = 1.1595m
        };
    }
}
