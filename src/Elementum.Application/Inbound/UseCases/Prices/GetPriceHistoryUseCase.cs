using Elementum.Application.DTOs;
using Elementum.Application.Mapping;
using Elementum.Application.Requests;
using Elementum.Domain.Ports.Outbound;
using Elementum.Domain.ValueObjects;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Interactor / Implementation of the price querying use case.
/// Uses <see cref="ValueTask{TResult}"/> to minimize managed heap allocations.
/// </summary>
public class GetPriceHistoryUseCase : IGetPriceHistoryUseCase
{
    private readonly IPriceHistoryReadRepository _repository;

    public GetPriceHistoryUseCase(IPriceHistoryReadRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryAllLatest(ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }

    public async ValueTask<PriceHistoryPageDto> GetBySymbolAsync(
        string symbol,
        string? currency = null,
        string? from = null,
        string? to = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (resolvedFrom, resolvedTo) = HistoryQueryLimits.ResolveRange(from, to, today);
        return await GetByDateRangeAsync(symbol, resolvedFrom, resolvedTo, currency, skip, take, ct);
    }

    public async ValueTask<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetPriceHistoryByMetalSymbolLatest(symbol, ct);
        return entity?.ToPriceHistoryDto();
    }

    public async ValueTask<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetPriceHistoryByMetalSymbolLatest(symbol, ct);
        return entity?.ToTradingPriceDto();
    }

    public async ValueTask<PriceHistoryPageDto> GetByDateRangeAsync(
        string symbol,
        DateOnly firstDate,
        DateOnly lastDate,
        string? currency = null,
        int skip = 0,
        int? take = null,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(currency))
            currency = Currency.FromCode(currency).Code;

        var effectiveSkip = HistoryQueryLimits.ClampSkip(skip);
        var effectiveTake = HistoryQueryLimits.ClampTake(take);

        var (entities, totalCount) = await _repository.GetPriceHistoryByMetalSymbolAndDateRangeAsync(
            symbol,
            firstDate,
            lastDate,
            currency,
            effectiveSkip,
            effectiveTake,
            ct);

        var items = entities.Select(e => e.ToPriceHistoryDto()).ToList();
        return new PriceHistoryPageDto
        {
            Items = items,
            Skip = effectiveSkip,
            Take = effectiveTake,
            TotalCount = totalCount,
            HasMore = effectiveSkip + items.Count < totalCount,
            From = firstDate,
            To = lastDate
        };
    }
}
