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

    /// <summary>
    /// GET /api/v1/prices/live — Real-time market overview for precious metals in EUR and USD.
    /// Supports HTTP caching with Cache-Control headers and ETag validation (304 Not Modified).
    /// </summary>
    [HttpGet("live")]
    public async Task<ActionResult<LiveMarketOverviewDto>> GetLivePrices(CancellationToken cancellationToken)
    {
        var overview = await _livePricesUseCase.GetLiveMarketOverviewAsync(cancellationToken);

        // HTTP Caching: Cache-Control & Weak ETag based on upstream snapshot timestamp
        Response.Headers.CacheControl = "public, max-age=30, stale-while-revalidate=60";
        var etag = $"W/\"{overview.Timestamp}\"";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch.ToString() == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(overview);
    }

    /// <summary>
    /// GET /api/v1/prices/trading/{symbol} (also /api/v1/prices/live/trading/{symbol}) —
    /// Real-time trading metrics and technical indicators (OHLC, spread, volatility, bullish/bearish status) for a precious metal.
    /// </summary>
    /// <param name="symbolRequest">Precious metal symbol (e.g. XAU, XAG, XPT, XPD).</param>
    /// <param name="currencyRequest">Optional currency query parameter: EUR (default) or USD. Other codes return 400 Bad Request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("trading/{symbol}")]
    [HttpGet("live/trading/{symbol}", Order = 2)]
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

        // HTTP Caching: Cache-Control & Weak ETag based on quote timestamp
        Response.Headers.CacheControl = "public, max-age=30, stale-while-revalidate=60";
        var etag = $"W/\"{dto.ReferenceTimestamp}\"";
        Response.Headers.ETag = etag;

        if (Request.Headers.IfNoneMatch.ToString() == etag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return Ok(dto);
    }
}
