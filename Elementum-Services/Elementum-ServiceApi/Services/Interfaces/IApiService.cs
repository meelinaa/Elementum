using Elementum.Shared.DTOs;

namespace Elementum_ServiceApi.Services.Interfaces
{
    /// <summary>
    /// Application service for the Elementum API. Exposes read operations for metals and price history.
    /// Controllers depend on this interface for testability and clear layering.
    /// </summary>
    public interface IApiService
    {
        /// <summary>Returns all metals as <see cref="MetalsDto"/> (Id, Symbol, Name).</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <returns>List of all metals as DTOs.</returns>
        Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken);

        /// <summary>Returns the latest price history entry per metal as <see cref="PriceHistoryDto"/> (for dashboard).</summary>
        Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken cancellationToken);

        /// <summary>Returns price history for a specific metal by symbol as <see cref="PriceHistoryDto"/>.</summary>
        Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns the most recent price history entry for a metal by symbol as <see cref="PriceHistoryDto"/>, or <c>null</c> if not found.</summary>
        Task<PriceHistoryDto?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns the latest price for a metal as <see cref="TradingPriceDto"/> (for TradingView: bid/ask, high/low, timestamps).</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The trading DTO or <c>null</c> if not found.</returns>
        Task<TradingPriceDto?> GetPriceHistoryTradingLatest(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns the latest price for a metal as <see cref="KaratPricesDto"/> (for KaratCalculatorView: price per gram 24k–10k).</summary>
        /// <param name="symbol">The metal symbol (e.g. XAU, XAG).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The karat DTO or <c>null</c> if not found.</returns>
        Task<KaratPricesDto?> GetPriceHistoryKaratLatest(string symbol, CancellationToken cancellationToken);

        /// <summary>Returns price history for a metal in date range as <see cref="PriceHistoryDto"/>.</summary>
        Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken cancellationToken);

        /// <summary>Returns aggregated price history for a metal as <see cref="PriceHistoryDto"/>.</summary>
        Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct);
    }
}
