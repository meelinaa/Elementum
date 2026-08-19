using Elementum.Application.DTOs;
using Elementum.Application.Mapping;
using Elementum.Domain.Ports.Outbound;

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

    public ValueTask<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, string? currency = null, CancellationToken ct = default)
    {
        var query = _repository.QueryPriceHistoryByMetalSymbol(symbol);
        if (!string.IsNullOrWhiteSpace(currency))
        {
            var cur = currency.Trim().ToUpperInvariant();
            query = query.Where(p => p.Currency == cur);
        }
        var entities = query.ToList();
        return ValueTask.FromResult<IEnumerable<PriceHistoryDto>>(entities.Select(e => e.ToPriceHistoryDto()));
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

    public ValueTask<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default)
    {
        var entities = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate).ToList();
        return ValueTask.FromResult<IEnumerable<PriceHistoryDto>>(entities.Select(e => e.ToPriceHistoryDto()));
    }

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryMetalData(symbol, aggregation, count, ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }
}
