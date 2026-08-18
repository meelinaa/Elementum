using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Metals;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Inbound.Controllers;

/// <summary>
/// Driving / Primary Adapter: REST API controller for Elementum (metals list and price history).
/// </summary>
[ApiController]
[Route("api/v1")]
public class ApiController(IGetPriceHistoryUseCase priceHistoryUseCase, IGetMetalsUseCase metalsUseCase, IGetDailyCandlesUseCase dailyCandlesUseCase) : ControllerBase
{
    private readonly IGetPriceHistoryUseCase _priceHistoryUseCase = priceHistoryUseCase ?? throw new ArgumentNullException(nameof(priceHistoryUseCase));
    private readonly IGetMetalsUseCase _metalsUseCase = metalsUseCase ?? throw new ArgumentNullException(nameof(metalsUseCase));
    private readonly IGetDailyCandlesUseCase _dailyCandlesUseCase = dailyCandlesUseCase ?? throw new ArgumentNullException(nameof(dailyCandlesUseCase));

    /// <summary>GET /api/v1/history/{symbol}/candles — daily candle summaries (Min, Max, Open, Close at 22:00) for charts.</summary>
    [HttpGet("history/{symbol}/candles", Order = 1)]
    [RequestTimeout("DataCruncher")]
    public async Task<ActionResult<IEnumerable<DailyPriceSummaryDto>>> GetDailyCandles(
        [FromRoute] SymbolRequest symbolRequest,
        [FromQuery] string currency = "USD",
        [FromQuery] string? fromDate = null,
        [FromQuery] string? toDate = null,
        CancellationToken cancellationToken = default)
    {
        DateOnly? from = fromDate != null && DateOnly.TryParse(fromDate, out var f) ? f : null;
        DateOnly? to = toDate != null && DateOnly.TryParse(toDate, out var t) ? t : null;

        var result = await _dailyCandlesUseCase.ExecuteAsync(symbolRequest.Symbol.Trim(), currency, from, to, cancellationToken);
        if (result.IsFailure)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = result.Error.Message,
                Instance = $"{Request.Method} {Request.Path}"
            });
        }

        return Ok(result.Value);
    }

    /// <summary>GET /api/v1/metals/all — all metals as <see cref="MetalsDto"/>.</summary>
    [HttpGet("metals/all")]
    public async Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken)
    {
        return await _metalsUseCase.GetAllAsync(cancellationToken);
    }

    /// <summary>GET /api/v1/history/all/latest — latest per metal as <see cref="PriceHistoryDto"/> for Dashboard.</summary>
    [HttpGet("history/all/latest")]
    public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
    {
        return await _priceHistoryUseCase.GetLatestAllAsync(cancellationToken);
    }

    /// <summary>GET /api/v1/history/{symbol} — price history for one metal as <see cref="PriceHistoryDto"/> array.</summary>
    [HttpGet("history/{symbol}", Order = 10)]
    [RequestTimeout("DataCruncher")]
    public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetPriceHistoryByMetalSymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
    {
        var historyData = await _priceHistoryUseCase.GetBySymbolAsync(symbolRequest.Symbol.Trim(), cancellationToken);
        return Ok(historyData);
    }

    /// <summary>GET /api/v1/history/{symbol}/latest — latest price history for one metal as <see cref="PriceHistoryDto"/>.</summary>
    [HttpGet("history/{symbol}/latest", Order = 5)]
    public async Task<ActionResult<PriceHistoryDto>> GetPriceHistoryByMetalSymbolLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
    {
        var dto = await _priceHistoryUseCase.GetLatestBySymbolAsync(symbolRequest.Symbol.Trim(), cancellationToken);
        if (dto == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No price history found for symbol '{symbolRequest.Symbol}'.",
                Instance = $"{Request.Method} {Request.Path}"
            });
        }
        return Ok(dto);
    }

    /// <summary>GET /api/v1/history/{symbol}/latest/trading — latest price as <see cref="TradingPriceDto"/>.</summary>
    [HttpGet("history/{symbol}/latest/trading", Order = 1)]
    public async Task<ActionResult<TradingPriceDto>> GetPriceHistoryTradingLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
    {
        var dto = await _priceHistoryUseCase.GetTradingLatestAsync(symbolRequest.Symbol.Trim(), cancellationToken);
        if (dto == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No price history found for symbol '{symbolRequest.Symbol}'.",
                Instance = $"{Request.Method} {Request.Path}"
            });
        }

        return Ok(dto);
    }

    /// <summary>GET /api/v1/history/{symbol}/latest/karat — latest price as <see cref="KaratPricesDto"/>.</summary>
    [HttpGet("history/{symbol}/latest/karat", Order = 1)]
    public async Task<ActionResult<KaratPricesDto>> GetPriceHistoryKaratLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
    {
        var dto = await _priceHistoryUseCase.GetKaratLatestAsync(symbolRequest.Symbol.Trim(), cancellationToken);
        if (dto == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No price history found for symbol '{symbolRequest.Symbol}'.",
                Instance = $"{Request.Method} {Request.Path}"
            });
        }

        return Ok(dto);
    }

    /// <summary>GET /api/v1/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range.</summary>
    [HttpGet("history/{symbol}/{firstDate}/{lastDate}", Order = 100)]
    [RequestTimeout("DataCruncher")]
    public async Task<IActionResult> GetPriceHistoryByMetalSymbolAndDateRange([FromRoute] SymbolRequest symbolRequest, [FromRoute] DateRangeRequest dateRangeRequest, CancellationToken ct)
    {
        var start = DateOnly.Parse(dateRangeRequest.FirstDate);
        var end = DateOnly.Parse(dateRangeRequest.LastDate);
        var historyData = await _priceHistoryUseCase.GetByDateRangeAsync(symbolRequest.Symbol.Trim(), start, end, ct);
        return Ok(historyData);
    }

    /// <summary>GET /api/v1/history/{symbol}/aggregated/{aggregation}/{count} — aggregated price history.</summary>
    [HttpGet("history/{symbol}/aggregated/{aggregation}/{count}", Order = 10)]
    [RequestTimeout("DataCruncher")]
    public async Task<IActionResult> GetPriceHistoryMetalData([FromRoute] SymbolRequest symbolRequest, [FromRoute] string aggregation, [FromRoute] int count, CancellationToken ct)
    {
        var historyData = await _priceHistoryUseCase.GetAggregatedAsync(symbolRequest.Symbol.Trim(), aggregation.Trim(), count, ct);
        return Ok(historyData);
    }
}
