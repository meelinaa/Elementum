using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data.Interfaces;
using Polly;

namespace Elementum.Infrastructure.Data.Resilience;

/// <summary>
/// Decorator that implements <see cref="IElementumDbContext"/> and wraps every call in a Polly retry policy.
/// When a transient MySQL error occurs, the operation is retried according to <see cref="ElementumDbContextResilienceOptions"/>.
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

    public Task<Metals?> GetMetalById(int id, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetMetalById(id, innerCt), ct);

    public Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetMetalBySymbol(symbol, innerCt), ct);

    public Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.IsDataAlreadyIngestedToday(innerCt), ct);

    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolLatest(symbol, innerCt), ct);

    public Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryAllLatest(innerCt), ct);

    public Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryMetalData(metalSymbol, aggregation, count, innerCt), ct);

    public Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default) =>
        ExecuteAsync(innerCt => _inner.SavePricesAsync(prices, innerCt), cancellationToken);

    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.AggregateDailySummaryAsync(date, innerCt), ct);

    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.PruneHourlyDataOlderThanAsync(thresholdUtc, innerCt), ct);

    public Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(string symbol, string currency, DateOnly? fromDate = null, DateOnly? toDate = null, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetDailySummariesAsync(symbol, currency, fromDate, toDate, innerCt), ct);

    private Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);

    private Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);
}
