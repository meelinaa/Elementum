using Elementum.Domain.Entities;
using Elementum.Domain.Models;
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
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
    }

    public IQueryable<Metals> QueryMetals() => _inner.QueryMetals();

    public IQueryable<PriceHistory> QueryPriceHistoryAll() => _inner.QueryPriceHistoryAll();

    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol) =>
        _inner.QueryPriceHistoryByMetalSymbol(symbol);

    public IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate) =>
        _inner.QueryPriceHistoryAllByDateRange(firstDate, lastDate);

    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate) =>
        _inner.QueryPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate);

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

    public Task SavePricesAsync(IReadOnlyList<DailyPrices> prices, CancellationToken cancellationToken = default) =>
        ExecuteAsync(innerCt => _inner.SavePricesAsync(prices, innerCt), cancellationToken);

    private Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);

    private Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);
}
