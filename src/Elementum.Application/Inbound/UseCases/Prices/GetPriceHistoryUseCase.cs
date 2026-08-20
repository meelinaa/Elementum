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

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, string? currency = null, CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryByMetalSymbolAsync(symbol, currency, ct);
        return entities.Select(e => e.ToPriceHistoryDto());
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

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryByMetalSymbolAndDateRangeAsync(symbol, firstDate, lastDate, currency: null, ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }

    public async ValueTask<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryMetalData(symbol, aggregation, count, ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }
}
