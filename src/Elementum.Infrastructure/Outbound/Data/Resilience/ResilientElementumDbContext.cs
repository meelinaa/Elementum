using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Polly;

namespace Elementum.Infrastructure.Outbound.Data.Resilience;

/// <summary>
/// Decorator that wraps <see cref="IPriceHistoryRepository"/> and retries every port call
/// through a Polly v8 resilience pipeline. Queries are materialized inside the inner repository
/// so retry covers the actual database work — there is no IQueryable passthrough.
/// </summary>
public sealed class ResilientElementumDbContext : IPriceHistoryRepository
{
    private readonly IPriceHistoryRepository _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientElementumDbContext(IPriceHistoryRepository inner, ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(pipeline);

        _inner = inner;
        _pipeline = pipeline;
    }

    public Task<IReadOnlyList<Metals>> GetMetalsAsync(CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetMetalsAsync(innerCt), ct);

    public Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.IsDataAlreadyIngestedToday(innerCt), ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAsync(
        string symbol,
        string? currency = null,
        CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolAsync(symbol, currency, innerCt), ct);

    public Task<(IReadOnlyList<PriceHistory> Items, int TotalCount)> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency,
        int skip,
        int take,
        CancellationToken ct = default) =>
        ExecuteAsync(
            innerCt => _inner.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
                symbol, firstDate, lastDate, currency, skip, take, innerCt),
            ct);

    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolLatest(symbol, innerCt), ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryAllLatest(innerCt), ct);

    public Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default) =>
        ExecuteAsync(innerCt => _inner.SavePricesAsync(prices, innerCt), cancellationToken);

    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.AggregateDailySummaryAsync(date, innerCt), ct);

    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.PruneHourlyDataOlderThanAsync(thresholdUtc, innerCt), ct);

    public Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetDailySummariesAsync(fromDate, toDate, innerCt), ct);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => await action(token), ct);

    private async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => { await action(token); }, ct);
}
