using Elementum.Application.DTOs;
using Elementum.Application.Mapping;
using Elementum.Domain.Common;
using Elementum.Domain.Errors;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Inbound.UseCases.Prices;

/// <summary>
/// Interactor implementation for querying daily candles (Min/Max/Open/Close at 22:00).
/// Uses mapper to decouple database entity structure from external API contract.
/// </summary>
public class GetDailyCandlesUseCase(
    IPriceHistoryReadRepository repository,
    ILogger<GetDailyCandlesUseCase> logger) : IGetDailyCandlesUseCase
{
    private readonly IPriceHistoryReadRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    private readonly ILogger<GetDailyCandlesUseCase> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Result<IReadOnlyList<DailyPriceSummaryDto>>> ExecuteAsync(
        string symbol,
        string currency = "USD",
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return Result.Failure<IReadOnlyList<DailyPriceSummaryDto>>(DomainErrors.Validation.InvalidSymbol);

        var metal = await _repository.GetMetalBySymbol(symbol, cancellationToken);
        if (metal == null)
            return Result.Failure<IReadOnlyList<DailyPriceSummaryDto>>(DomainErrors.Metals.NotFoundBySymbol(symbol));

        var summaries = await _repository.GetDailySummariesAsync(symbol, currency, fromDate, toDate, cancellationToken);

        var dtos = summaries.Select(s => s.ToDailyPriceSummaryDto(metal.Symbol, metal.Name)).ToList();

        return Result.Success<IReadOnlyList<DailyPriceSummaryDto>>(dtos);
    }
}
