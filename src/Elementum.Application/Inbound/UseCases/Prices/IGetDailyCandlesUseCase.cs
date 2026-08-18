using Elementum.Application.DTOs;
using Elementum.Domain.Common;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Inbound Port: Use Case for querying daily consolidated price candles (Min/Max/Open/Close at 22:00).
/// </summary>
public interface IGetDailyCandlesUseCase
{
    Task<Result<IReadOnlyList<DailyPriceSummaryDto>>> ExecuteAsync(
        string symbol,
        string currency = "USD",
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);
}
