using Elementum.Application.DTOs;
using Elementum.Domain.Common;
using Elementum.Domain.Errors;
using Elementum.Domain.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.UseCases.Prices;

/// <summary>
/// Interactor implementation for querying daily candles (Min/Max/Open/Close at 22:00).
/// </summary>
public class GetDailyCandlesUseCase : IGetDailyCandlesUseCase
{
    private readonly IPriceHistoryRepository _repository;
    private readonly ILogger<GetDailyCandlesUseCase> _logger;

    public GetDailyCandlesUseCase(
        IPriceHistoryRepository repository,
        ILogger<GetDailyCandlesUseCase> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

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

        var dtos = summaries.Select(s => new DailyPriceSummaryDto
        {
            Id = s.Id,
            MetalId = s.MetalId,
            Symbol = metal.Symbol,
            MetalName = metal.Name,
            Currency = s.Currency,
            EntryDate = s.EntryDate,
            OpenPrice = s.OpenPrice,
            HighPrice = s.HighPrice,
            LowPrice = s.LowPrice,
            ClosePrice = s.ClosePrice,
            ExchangeRateUsdEur = s.ExchangeRateUsdEur
        }).ToList();

        return Result.Success<IReadOnlyList<DailyPriceSummaryDto>>(dtos);
    }
}
