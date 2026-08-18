using Elementum.Application.DTOs;

namespace Elementum.Application.Inbound.UseCases.Metals;

/// <summary>
/// Primary / Inbound Port: Queries precious metals master data.
/// </summary>
public interface IGetMetalsUseCase
{
    Task<IEnumerable<MetalsDto>> GetAllAsync(CancellationToken ct = default);
    Task<MetalsDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<MetalsDto?> GetBySymbolAsync(string symbol, CancellationToken ct = default);
}
