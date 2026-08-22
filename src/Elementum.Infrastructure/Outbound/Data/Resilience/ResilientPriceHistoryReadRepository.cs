using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Polly;

namespace Elementum.Infrastructure.Outbound.Data.Resilience;

/// <summary>
/// Secondary / Driven Outbound Read Decorator: Wraps <see cref="IPriceHistoryReadRepository"/>
/// and executes every query through a Polly v8 resilience pipeline.
/// Follows Interface Segregation Principle (ISP) / CQRS for dedicated query-side resilience.
/// </summary>
public sealed class ResilientPriceHistoryReadRepository : IPriceHistoryReadRepository
{
    private readonly IPriceHistoryReadRepository _inner;
    private readonly ResiliencePipeline _pipeline;

    public ResilientPriceHistoryReadRepository(IPriceHistoryReadRepository inner, ResiliencePipeline pipeline)
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

    public Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default) =>
        ExecuteAsync(innerCt => _inner.GetDailySummariesAsync(fromDate, toDate, innerCt), ct);

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct) =>
        await _pipeline.ExecuteAsync(async token => await action(token), ct);
}
