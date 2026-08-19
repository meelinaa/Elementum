using Elementum.Application.DTOs;
using Elementum.Application.Inbound.UseCases.Prices;
using Elementum.Application.Requests;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Elementum.Api.Inbound.Controllers;

/// <summary>
/// Driving / Primary Adapter: REST API controller for historical price data.
/// </summary>
[ApiController]
[Route("api/v1/history")]
public class PriceHistoryController : ControllerBase
{
    private readonly IGetPriceHistoryUseCase _priceHistoryUseCase;

    public PriceHistoryController(IGetPriceHistoryUseCase priceHistoryUseCase)
    {
        ArgumentNullException.ThrowIfNull(priceHistoryUseCase);
        _priceHistoryUseCase = priceHistoryUseCase;
    }

    /// <summary>GET /api/v1/history/{symbol}?currency={currency} — price history for one metal as <see cref="PriceHistoryDto"/> array.</summary>
    [HttpGet("{symbol}", Order = 10)]
    [RequestTimeout("DataCruncher")]
    public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetPriceHistoryByMetalSymbol(
        [FromRoute] SymbolRequest symbolRequest,
        [FromQuery] string? currency,
        CancellationToken cancellationToken)
    {
        var historyData = await _priceHistoryUseCase.GetBySymbolAsync(symbolRequest.Symbol.Trim(), currency, cancellationToken);
        return Ok(historyData);
    }
}
