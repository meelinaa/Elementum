using Elementum.Application.DTOs;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Primary / Inbound Port: Queries live 5-minute precious metal market prices and trading analytics.
/// </summary>
public interface ILivePricesUseCase
{
    /// <summary>Returns 5-minute live market overview for all supported metals (USD and EUR).</summary>
    Task<LiveMarketOverviewDto> GetLiveMarketOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns trading analysis for a specific metal symbol and currency (EUR or USD).</summary>
    Task<TradingPriceDto?> GetLiveTradingAnalysisAsync(string symbol, string currency = "EUR", CancellationToken cancellationToken = default);
}
