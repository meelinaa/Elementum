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
    public Task<IEnumerable<Metals>> GetMetalsAll(CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetMetalsAll(ct), ct);

    /// <inheritdoc />
    public Task<Metals?> GetMetalById(int id, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetMetalById(id, ct), ct);

    /// <inheritdoc />
    public Task<Metals?> GetMetalBySymbol(string symbol, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetMetalBySymbol(symbol, ct), ct);

    /// <inheritdoc />
    public Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct) =>
        ExecuteAsync(ct => _inner.IsDataAlreadyIngestedToday(ct), ct);

    /// <inheritdoc />
    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryByMetalSymbolLatest(symbol, ct), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryAll(ct), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryAllLatest(ct), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryByMetalSymbol(symbol, ct), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryAllByDateRange(DateOnly firstDate, DateOnly lastDate, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryAllByDateRange(firstDate, lastDate, ct), ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate, ct), ct);

    /// <summary>Runs the delegate under the retry policy so transient DB failures are retried automatically.</summary>
    private Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        _policy.ExecuteAsync(action, ct);

    /// <inheritdoc />
    public Task<IEnumerable<PriceHistory>> GetPriceHistoryMetalData(string metalSymbol, string aggregation, int count, CancellationToken ct) =>
        ExecuteAsync(ct => _inner.GetPriceHistoryMetalData(metalSymbol, aggregation, count, ct), ct);
}
