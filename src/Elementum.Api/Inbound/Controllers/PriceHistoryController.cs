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
    /// bounded tick page as <see cref="PriceHistoryPageDto"/>.
    /// </summary>
    [HttpGet("{symbol}", Order = 10)]
    [RequestTimeout("DataCruncher")]
    public async Task<ActionResult<PriceHistoryPageDto>> GetPriceHistoryByMetalSymbol(
        [FromRoute] SymbolRequest symbolRequest,
        [FromQuery] CurrencyRequest currencyRequest,
        [FromQuery] DateRangeRequest dateRange,
        [FromQuery] HistoryPageRequest page,
        CancellationToken cancellationToken)
    {
        var historyData = await _priceHistoryUseCase.GetBySymbolAsync(
            symbolRequest.Symbol.Trim(),
            currencyRequest.Currency,
            dateRange.From,
            dateRange.To,
            page.Skip,
            page.Take,
            cancellationToken);
        return Ok(historyData);
    }
}
