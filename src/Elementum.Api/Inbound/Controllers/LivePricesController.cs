using Elementum.Api.Exceptions;
using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Inbound.Controllers;

/// <summary>
/// Driving / Primary Adapter: REST API controller for 5-minute live prices and trading analysis.
/// </summary>
[ApiController]
[Route("api/v1/prices")]
public class LivePricesController : ControllerBase
{
    private readonly ILivePricesUseCase _livePricesUseCase;

    public LivePricesController(ILivePricesUseCase livePricesUseCase)
    {
        ArgumentNullException.ThrowIfNull(livePricesUseCase);
        _livePricesUseCase = livePricesUseCase;
    }

    /// <summary>GET /api/v1/prices/live — 5-minute live market overview for Dashboard.</summary>
    [HttpGet("live")]
    public async Task<ActionResult<LiveMarketOverviewDto>> GetLivePrices(CancellationToken cancellationToken)
    {
        var overview = await _livePricesUseCase.GetLiveMarketOverviewAsync(cancellationToken);
        return Ok(overview);
    }

    /// <summary>GET /api/v1/prices/live/trading/{symbol} — 5-minute live trading analysis for TradingView.</summary>
    [HttpGet("live/trading/{symbol}")]
    public async Task<ActionResult<TradingPriceDto>> GetLiveTradingAnalysis(
        [FromRoute] SymbolRequest symbolRequest,
        [FromQuery] CurrencyRequest currencyRequest,
        CancellationToken cancellationToken = default)
    {
        var dto = await _livePricesUseCase.GetLiveTradingAnalysisAsync(
            symbolRequest.Symbol.Trim(),
            currencyRequest.ForTrading(),
            cancellationToken);
        if (dto == null)
        {
            var problem = ExceptionStatusMapper.ForNotFound().ToProblemDetails(
                detail: $"No trading analysis found for symbol '{symbolRequest.Symbol}'.",
                instance: $"{Request.Method} {Request.Path}");
            return NotFound(problem);
        }

        return Ok(dto);
    }
}
