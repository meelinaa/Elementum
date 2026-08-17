using Elementum.Application.DTOs;
using Elementum.Application.Mapping;
using Elementum.Domain.Ports.Outbound;

namespace Elementum.Application.UseCases.Prices;

/// <summary>
/// Interactor / Implementation of the price querying use case.
/// </summary>
public class GetPriceHistoryUseCase : IGetPriceHistoryUseCase
{
    private readonly IPriceHistoryRepository _repository;

    public GetPriceHistoryUseCase(IPriceHistoryRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetLatestAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryAllLatest(ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        var entities = _repository.QueryPriceHistoryByMetalSymbol(symbol).ToList();
        return entities.Select(e => e.ToPriceHistoryDto());
    }

    public async Task<PriceHistoryDto?> GetLatestBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetPriceHistoryByMetalSymbolLatest(symbol, ct);
        return entity?.ToPriceHistoryDto();
    }

    public async Task<TradingPriceDto?> GetTradingLatestAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetPriceHistoryByMetalSymbolLatest(symbol, ct);
        return entity?.ToTradingPriceDto();
    }

    public async Task<KaratPricesDto?> GetKaratLatestAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetPriceHistoryByMetalSymbolLatest(symbol, ct);
        return entity?.ToKaratPricesDto();
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetByDateRangeAsync(string symbol, DateOnly firstDate, DateOnly lastDate, CancellationToken ct = default)
    {
        var entities = _repository.QueryPriceHistoryByMetalSymbolAndDateRange(symbol, firstDate, lastDate).ToList();
        return entities.Select(e => e.ToPriceHistoryDto());
    }

    public async Task<IEnumerable<PriceHistoryDto>> GetAggregatedAsync(string symbol, string aggregation, int count, CancellationToken ct = default)
    {
        var entities = await _repository.GetPriceHistoryMetalData(symbol, aggregation, count, ct);
        return entities.Select(e => e.ToPriceHistoryDto());
    }
}
