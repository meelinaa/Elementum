using Elementum.Application.DTOs;
using Elementum.Application.Mapping;
using Elementum.Domain.Ports.Outbound;

namespace Elementum.Application.Inbound.UseCases.Metals;

/// <summary>
/// Interactor / Implementation of the metals master data use case.
/// </summary>
public class GetMetalsUseCase(IPriceHistoryReadRepository repository) : IGetMetalsUseCase
{
    private readonly IPriceHistoryReadRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<MetalsDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = _repository.QueryMetals().ToList();
        return entities.Select(e => e.ToDto());
    }

    public async Task<MetalsDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetMetalById(id, ct);
        return entity?.ToDto();
    }

    public async Task<MetalsDto?> GetBySymbolAsync(string symbol, CancellationToken ct = default)
    {
        var entity = await _repository.GetMetalBySymbol(symbol, ct);
        return entity?.ToDto();
    }
}
