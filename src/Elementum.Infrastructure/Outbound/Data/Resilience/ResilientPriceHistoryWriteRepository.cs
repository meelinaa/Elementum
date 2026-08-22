using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Polly;

namespace Elementum.Infrastructure.Outbound.Data.Resilience;

/// <summary>
/// Secondary / Driven Outbound Write Decorator: Wraps <see cref="IPriceHistoryWriteRepository"/>
/// and executes every command/mutation through a Polly v8 resilience pipeline.
/// Follows Interface Segregation Principle (ISP) / CQRS for dedicated write-side resilience.
/// </summary>
public sealed class ResilientPriceHistoryWriteRepository : IPriceHistoryWriteRepository
{
    private readonly IPriceHistoryWriteRepository _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientPriceHistoryWriteRepository(IPriceHistoryWriteRepository inner, ResiliencePipeline pipeline)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(pipeline);

        _inner = inner;
        _pipeline = pipeline;
    }

    public Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default) =>
        ExecuteAsync(innerCt => _inner.SavePricesAsync(prices, innerCt), cancellationToken);

    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.AggregateDailySummaryAsync(date, innerCt), ct);

    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.PruneHourlyDataOlderThanAsync(thresholdUtc, innerCt), ct);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => await action(token), ct);

    private async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => { await action(token); }, ct);
}
