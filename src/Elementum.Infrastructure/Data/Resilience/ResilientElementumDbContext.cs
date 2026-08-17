using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;
using Polly;

namespace Elementum.Infrastructure.Data.Resilience;

/// <summary>
/// Decorator that implements <see cref="IElementumDbContext"/> and wraps every call in a Polly retry policy.
/// When a transient MySQL error occurs (e.g. connection lost, deadlock), the operation is retried according to <see cref="ElementumDbContextResilienceOptions"/>.
/// Registered in DI only when AddElementumDbContext is called with <c>configureResilience</c> (API today; Worker can use the same later).
/// </summary>
/// <remarks>
/// <see cref="IQueryable{T}"/> members are forwarded without wrapping; execution occurs when the caller runs e.g. <c>ToListAsync</c> on the returned query.
/// </remarks>
public sealed class ResilientElementumDbContext : IElementumDbContext
{
    private readonly IElementumDbContext _inner;
    private readonly IAsyncPolicy _policy;

    /// <summary>Creates a resilient decorator around the given context. All interface methods are executed via <paramref name="policy"/>.</summary>
    public ResilientElementumDbContext(IElementumDbContext inner, IAsyncPolicy policy)
    {
        _inner = inner;
        _policy = policy;
    }

    /// <inheritdoc />
    public IQueryable<Metals> QueryMetals() => _inner.QueryMetals();

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAll() => _inner.QueryPriceHistoryAll();

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbol(string symbol) =>
        _inner.QueryPriceHistoryByMetalSymbol(symbol);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate) =>
        _inner.QueryPriceHistoryAllByDateRange(firstDate, lastDate);

    /// <inheritdoc />
    public IQueryable<PriceHistory> QueryPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate) =>
        _inner.QueryPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate);

    /// <inheritdoc />
    public Task<Metals?> GetMetalById(int id, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetMetalById(id, innerCt), ct);

    /// <inheritdoc />
    public Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetMetalBySymbol(symbol, innerCt), ct);

    /// <inheritdoc />
    public Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.IsDataAlreadyIngestedToday(innerCt), ct);

    /// <inheritdoc />
    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryByMetalSymbolLatest(symbol, innerCt), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryAllLatest(innerCt), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct) =>
        ExecuteAsync(innerCt => _inner.GetPriceHistoryMetalData(metalSymbol, aggregation, count, innerCt), ct);

    /// <summary>Runs the delegate under the retry policy so transient DB failures are retried automatically.</summary>
    private Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);
}
