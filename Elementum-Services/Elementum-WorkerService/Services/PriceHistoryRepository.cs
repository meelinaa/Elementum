using Elementum.Infrastructure.Data;
using Elementum.Shared.Objects;
using Elementum_WorkerService.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elementum_WorkerService.Services;

/// <summary>
/// Persists API price data into the <c>price_history</c> table. Resolves <c>metal_id</c> from the <c>metals</c> table by symbol.
/// </summary>
public class PriceHistoryRepository : IPriceHistoryRepository
{
    private readonly ILogger<PriceHistoryRepository> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Injects logger and scope factory to resolve a scoped <see cref="ElementumDbContext"/> per save.</summary>
    public PriceHistoryRepository(ILogger<PriceHistoryRepository> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    /// <summary>
    /// Maps API <see cref="DailyPrices"/> to <see cref="PriceHistory"/> entities and saves them. Skips entries whose metal symbol is not found in <c>metals</c>.
    /// Handles duplicate-key violations (same metal/currency/date) by logging a warning and not throwing.
    /// </summary>
    /// <param name="prices">Prices returned from the API (one per metal).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default)
    {
        if (prices.Count == 0)
            return;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ElementumDbContext>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var metalsBySymbol = await db.Metals.ToDictionaryAsync(m => m.Symbol, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var api in prices)
        {
            if (string.IsNullOrEmpty(api.Metal) || !metalsBySymbol.TryGetValue(api.Metal, out var metal))
            {
                _logger.LogWarning("Skipping price for unknown metal: {Metal}", api.Metal);
                continue;
            }
            var row = new PriceHistory
            {
                MetalId = metal.Id,
                Currency = api.Currency,
                Exchange = api.Exchange,
                Symbol = api.Symbol,
                ReferenceTimestamp = api.Timestamp,
                OpenTime = api.OpenTime,
                EntryDate = today,
                Price = api.Price,
                PrevClosePrice = api.PrevClosePrice,
                OpenPrice = api.OpenPrice,
                LowPrice = api.LowPrice,
                HighPrice = api.HighPrice,
                Ch = api.Ch,
                Chp = api.Chp,
                Ask = api.Ask,
                Bid = api.Bid,
                PriceGram24k = api.PriceGram24k,
                PriceGram22k = api.PriceGram22k,
                PriceGram21k = api.PriceGram21k,
                PriceGram20k = api.PriceGram20k,
                PriceGram18k = api.PriceGram18k,
                PriceGram16k = api.PriceGram16k,
                PriceGram14k = api.PriceGram14k,
                PriceGram10k = api.PriceGram10k
            };
            db.PriceHistory.Add(row);
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Saved {Count} price(s) to price_history.", prices.Count);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("Duplicate") == true)
        {
            _logger.LogWarning("One or more rows already exist for today (unique metal_id/currency/entry_date). Skipping duplicate.");
        }
    }
}
