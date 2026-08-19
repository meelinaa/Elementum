using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Http;
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
        [FromQuery] string currency = "EUR",
        CancellationToken cancellationToken = default)
    {
        var dto = await _livePricesUseCase.GetLiveTradingAnalysisAsync(symbolRequest.Symbol.Trim(), currency, cancellationToken);
        if (dto == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No trading analysis found for symbol '{symbolRequest.Symbol}'.",
                Instance = $"{Request.Method} {Request.Path}"
            });
        }

        return Ok(dto);
    }
}
