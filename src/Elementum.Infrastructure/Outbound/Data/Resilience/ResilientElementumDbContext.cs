using Elementum.Domain.Entities;
using Elementum.Domain.Ports.Outbound;
using Polly;

namespace Elementum.Infrastructure.Outbound.Data.Resilience;

/// <summary>
/// Composite Decorator: Combines <see cref="ResilientPriceHistoryReadRepository"/> and <see cref="ResilientPriceHistoryWriteRepository"/>
/// into the unified <see cref="IPriceHistoryRepository"/> port.
/// Preserves Interface Segregation Principle (ISP) by delegating to segregated read and write decorators.
/// </summary>
public sealed class ResilientElementumDbContext : IPriceHistoryRepository
{
    private readonly IPriceHistoryReadRepository _readRepo;
    private readonly IPriceHistoryWriteRepository _writeRepo;

    public ResilientElementumDbContext(IPriceHistoryRepository inner, ResiliencePipeline pipeline)
        : this(new ResilientPriceHistoryReadRepository(inner, pipeline),
               new ResilientPriceHistoryWriteRepository(inner, pipeline))
    {
    }

    public ResilientElementumDbContext(
        IPriceHistoryReadRepository readRepo,
        IPriceHistoryWriteRepository writeRepo)
    {
        ArgumentNullException.ThrowIfNull(readRepo);
        ArgumentNullException.ThrowIfNull(writeRepo);

        _readRepo = readRepo;
        _writeRepo = writeRepo;
    }

    // Read Port Delegation
    public Task<IReadOnlyList<Metals>> GetMetalsAsync(CancellationToken ct = default) =>
        _readRepo.GetMetalsAsync(ct);

    public Task<bool> IsDataAlreadyIngestedToday(CancellationToken ct = default) =>
        _readRepo.IsDataAlreadyIngestedToday(ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryByMetalSymbolAsync(
        string symbol,
        string? currency = null,
        CancellationToken ct = default) =>
        _readRepo.GetPriceHistoryByMetalSymbolAsync(symbol, currency, ct);

    public Task<(IReadOnlyList<PriceHistory> Items, int TotalCount)> GetPriceHistoryByMetalSymbolAndDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency,
        int skip,
        int take,
        CancellationToken ct = default) =>
        _readRepo.GetPriceHistoryByMetalSymbolAndDateRangeAsync(symbol, firstDate, lastDate, currency, skip, take, ct);

    public Task<PriceHistory?> GetPriceHistoryByMetalSymbolLatest(string symbol, CancellationToken ct) =>
        _readRepo.GetPriceHistoryByMetalSymbolLatest(symbol, ct);

    public Task<IReadOnlyList<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken ct) =>
        _readRepo.GetPriceHistoryAllLatest(ct);

    public Task<IReadOnlyList<DailyPriceSummary>> GetDailySummariesAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default) =>
        _readRepo.GetDailySummariesAsync(fromDate, toDate, ct);

    // Write Port Delegation
    public Task SavePricesAsync(IReadOnlyList<PriceHistory> prices, CancellationToken cancellationToken = default) =>
        _writeRepo.SavePricesAsync(prices, cancellationToken);

    public Task AggregateDailySummaryAsync(DateOnly date, CancellationToken ct = default) =>
        _writeRepo.AggregateDailySummaryAsync(date, ct);

    public Task<int> PruneHourlyDataOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) =>
        _writeRepo.PruneHourlyDataOlderThanAsync(thresholdUtc, ct);
}
