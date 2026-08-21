using Elementum.Domain.Entities;
using Elementum.Infrastructure.Outbound.Data.Interfaces;
using Polly;

namespace Elementum.Infrastructure.Outbound.Data.Resilience;

/// <summary>
/// Decorator that implements <see cref="IElementumDbContext"/> and wraps every port call in a Polly retry policy.
/// Queries are materialized inside the inner repository so retry covers the actual database work — there is no IQueryable passthrough.
/// </summary>
public sealed class ResilientElementumDbContext : IElementumDbContext
{
    private readonly IElementumDbContext _inner;
    private readonly IAsyncPolicy _policy;

    public ResilientElementumDbContext(IElementumDbContext inner, IAsyncPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(policy);

        _inner = inner;
        _policy = policy;
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

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolAndDateRangeAsync(symbol, firstDate, lastDate, currency, innerCt), ct);

    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolLatest(symbol, innerCt), ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryAllLatest(innerCt), ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryMetalData(metalSymbol, aggregation, count, innerCt), ct);

    public Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default) =>
        ExecuteAsync(innerCt => _inner.SavePricesAsync(prices, innerCt), cancellationToken);

    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.AggregateDailySummaryAsync(date, innerCt), ct);

    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.PruneHourlyDataOlderThanAsync(thresholdUtc, innerCt), ct);

    public Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetDailySummariesAsync(fromDate, toDate, innerCt), ct);

    private Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);

    private Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);
}
