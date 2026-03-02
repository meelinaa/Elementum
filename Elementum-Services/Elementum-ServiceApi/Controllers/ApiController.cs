using Elementum.Shared.Objects;
using Elementum_ServiceApi.Models;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Elementum_ServiceApi.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class FrontendService : ControllerBase
    {
        private readonly IApiService _apiService;

        public FrontendService(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>GET /api/metals — all metals. Returns JSON array.</summary>
        [HttpGet("metals/all")]
        public async Task<IEnumerable<Metals>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _apiService.GetAllMetals(cancellationToken);
        }

        /// <summary>GET /api/metals/{symbol} — single metal by symbol. Returns 404 if not found.</summary>
        [HttpGet("metals/{symbol}")]
        public async Task<ActionResult<Metals>> GetMetalBySymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var metal = await _apiService.GetMetalBySymbol(symbolRequest.Symbol.Trim(), cancellationToken);
            if (metal == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No metal found with symbol '{symbolRequest.Symbol}'.",
                    Instance = $"{Request.Method} {Request.Path}"
                });
            }

            return metal;
        }

        /// <summary>GET /api/history — all price history. Returns JSON array.</summary>
        [HttpGet("history/all")]
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken cancellationToken)
        {
            return await _apiService.GetPriceHistoryAll(cancellationToken);
        }

        [HttpGet("history/all/latest")]
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
        {
            return await _apiService.GetPriceHistoryAllLatest(cancellationToken);
        }

        /// <summary>GET /api/history/{symbol} — price history for one metal. Returns JSON array.</summary>
        [HttpGet("history/{symbol}")]
        public async Task<ActionResult<IEnumerable<PriceHistory>>> GetPriceHistoryByMetalSymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryByMetalSymbol(symbolRequest.Symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>GET /api/history/{symbol} — price history for one metal. Returns JSON array.</summary>
        [HttpGet("history/{symbol}/latest")]
        public async Task<ActionResult<IEnumerable<PriceHistory>>> GetPriceHistoryByMetalSymbolLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryByMetalSymbolLatest(symbolRequest.Symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>
        /// GET /api/v1/history/all/{firstDate}/{lastDate} — history in date range.
        /// Returns JSON array. Dates: yyyy-MM-dd. Uses request validation via <see cref="DateRangeRequest"/>.
        /// </summary>
        [HttpGet("history/all/{firstDate}/{lastDate}")]
        public async Task<IActionResult> GetPriceHistoryAllByDateRange([FromRoute] DateRangeRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var start = DateOnly.Parse(request.FirstDate);
            var end = DateOnly.Parse(request.LastDate);
            var historyData = await _apiService.GetPriceHistoryAllByDateRange(start, end, ct);
            return Ok(historyData);
        }

        /// <summary>GET /api/v1/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range. Uses <see cref="SymbolRequest"/> and <see cref="DateRangeRequest"/>.</summary>
        [HttpGet("history/{symbol}/{firstDate}/{lastDate}")]
        public async Task<IActionResult> GetPriceHistoryByMetalSymbolAndDateRange([FromRoute] SymbolRequest symbolRequest, [FromRoute] DateRangeRequest dateRangeRequest, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var start = DateOnly.Parse(dateRangeRequest.FirstDate);
            var end = DateOnly.Parse(dateRangeRequest.LastDate);
            var historyData = await _apiService.GetPriceHistoryByMetalSymbolAndDateRange(symbolRequest.Symbol.Trim(), start, end, ct);
            return Ok(historyData);
        }
    }
}
