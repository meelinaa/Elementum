using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Elementum.Infrastructure.Data.Services;

/// <summary>
/// Service implementation for daily price summary (candle) aggregation and real-time tick updates.
/// Handles optimistic concurrency conflicts transparently via entity reload and reconciliation.
/// </summary>
public class DailyCandleAggregator : IDailyCandleAggregator
{
    /// <inheritdoc />
    public async Task AggregateDailySummaryAsync(ElementumDbContext db, DateOnly date, CancellationToken ct = default)
    {
        var dayTicks = await db.PriceHistory
            .Where(p => p.EntryDate == date)
            .OrderBy(p => p.ReferenceTimestamp)
            .ToListAsync(ct);

        if (dayTicks.Count == 0)
            return;

        var groups = dayTicks.GroupBy(p => new { p.MetalId, p.Currency });

        foreach (var g in groups)
        {
            var openPrice = g.First().Price;
            var highPrice = g.Max(p => p.Price);
            var lowPrice = g.Min(p => p.Price);
            var closePrice = g.Last().Price;

            var existing = await db.DailyPriceSummaries
                .FirstOrDefaultAsync(s => s.MetalId == g.Key.MetalId && s.Currency == g.Key.Currency && s.EntryDate == date, ct);

            if (existing == null)
            {
                var summary = DailyPriceSummary.Create(
                    metalId: g.Key.MetalId,
                    currency: g.Key.Currency,
                    entryDate: date,
                    openPrice: openPrice,
                    highPrice: highPrice,
                    lowPrice: lowPrice,
                    closePrice: closePrice);

                db.DailyPriceSummaries.Add(summary);
            }
            else
            {
                existing.OpenPrice = openPrice;
                existing.HighPrice = highPrice;
                existing.LowPrice = lowPrice;
                existing.ClosePrice = closePrice;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic concurrency conflict: reload affected tracked entries and re-save
            foreach (var entry in db.ChangeTracker.Entries<DailyPriceSummary>())
            {
                await entry.ReloadAsync(ct);
            }
            await db.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task UpdateSummaryForTickAsync(
        ElementumDbContext db,
        int metalId,
        string currency,
        DateOnly date,
        decimal price,
        decimal exchangeRateUsdEur,
        bool isCloseHour,
        CancellationToken ct = default)
    {
        var dailySummary = await db.DailyPriceSummaries
            .FirstOrDefaultAsync(s => s.MetalId == metalId && s.Currency == currency && s.EntryDate == date, ct);

        if (dailySummary == null)
        {
            dailySummary = DailyPriceSummary.Create(
                metalId: metalId,
                currency: currency,
                entryDate: date,
                openPrice: price,
                highPrice: price,
                lowPrice: price,
                closePrice: price,
                exchangeRateUsdEur: exchangeRateUsdEur);

            db.DailyPriceSummaries.Add(dailySummary);
        }
        else
        {
            dailySummary.ApplyPriceTick(price, exchangeRateUsdEur, isClosePrice: isCloseHour);
        }
    }
}
