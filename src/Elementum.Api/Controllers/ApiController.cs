using Elementum.Application.DTOs;
using Elementum.Application.Requests;
using Elementum.Application.UseCases.Metals;
using Elementum.Application.UseCases.Prices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Controllers;

/// <summary>
/// Driving / Primary Adapter: REST API controller for Elementum (metals list and price history).
/// </summary>
[ApiController]
[Route("api/v1")]
public class ApiController : ControllerBase
{
    private readonly IGetPriceHistoryUseCase _priceHistoryUseCase;
    private readonly IGetMetalsUseCase _metalsUseCase;

    public ApiController(IGetPriceHistoryUseCase priceHistoryUseCase, IGetMetalsUseCase metalsUseCase)
    {
        _priceHistoryUseCase = priceHistoryUseCase ?? throw new ArgumentNullException(nameof(priceHistoryUseCase));
        _metalsUseCase = metalsUseCase ?? throw new ArgumentNullException(nameof(metalsUseCase));
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
