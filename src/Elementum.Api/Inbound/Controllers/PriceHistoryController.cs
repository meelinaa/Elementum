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

    /// <summary>
    /// GET /api/v1/history/{symbol}?from=&amp;to=&amp;take=&amp;skip=&amp;currency= —
    /// Returns a bounded historical tick page for a precious metal.
    /// <para>
    /// <b>Currency Behavior:</b> When <c>currency</c> is omitted, historical records across <b>both EUR and USD</b> are returned.
    /// Specifying <c>currency=EUR</c> or <c>currency=USD</c> strictly filters by that currency.
    /// </para>
    /// </summary>
    /// <param name="symbolRequest">Precious metal symbol (e.g. XAU, XAG, XPT, XPD).</param>
    /// <param name="query">Pagination and date-range filters (from, to, skip, take, currency).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{symbol}", Order = 10)]
    [RequestTimeout("DataCruncher")]
    public async Task<ActionResult<PriceHistoryPageDto>> GetPriceHistoryByMetalSymbol(
        [FromRoute] SymbolRequest symbolRequest,
        [FromQuery] HistoryQueryRequest query,
        CancellationToken cancellationToken)
    {
        var historyData = await _priceHistoryUseCase.GetBySymbolAsync(
            symbolRequest.Symbol.Trim(),
            query.NormalizedCurrency,
            query.From,
            query.To,
            query.Skip,
            query.Take,
            cancellationToken);

        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(historyData);
    }
}
