using Elementum.Application.DTOs;

namespace Elementum.Cli.Api;

/// <summary>
/// HTTP client for the Elementum REST API. Instance-based so timeouts, cache, and handlers are injectable.
/// </summary>
public interface IHttpCall
{
    Task<LiveMarketOverviewDto?> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default);

    Task<TradingPriceDto?> GetPriceHistoryTradingLatestAsync(
        string metalSymbol,
        string currency = "EUR",
        CancellationToken cancellationToken = default);

    Task<List<PriceHistoryDto>?> GetPriceHistoryMetalAsync(
        string metalSymbol,
        string currency = "EUR",
        CancellationToken cancellationToken = default);

    void ClearCache();
}
