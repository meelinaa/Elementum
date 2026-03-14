using Elementum.Shared.DTOs;
using Elementum_ServiceApi.RequestModels;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Elementum_ServiceApi.Controllers
{
    /// <summary>
    /// REST API controller for Elementum: metals list and price history (latest, by symbol, by date range, aggregated, trading/karat views).
    /// All responses use DTOs; base route is <c>api/v1</c>.
    /// </summary>
    [ApiController]
    [Route("api/v1")]
    public class ApiController : ControllerBase
    {
        private readonly IApiService _apiService;

        /// <summary>Injects the application service for metals and price history.</summary>
        public ApiController(IApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>GET /api/v1/metals/all — all metals as <see cref="MetalsDto"/> (Id, Symbol, Name).</summary>
        [HttpGet("metals/all")]
        public async Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _apiService.GetAllMetals(cancellationToken);
        }

        /// <summary>GET /api/v1/history/all/latest — latest per metal as <see cref="PriceHistoryDto"/> for Dashboard.</summary>
        [HttpGet("history/all/latest")]
        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
        {
            return await _apiService.GetPriceHistoryAllLatest(cancellationToken);
        }

        /// <summary>GET /api/v1/history/{symbol} — price history for one metal as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}", Order = 10)]
        [RequestTimeout("DataCruncher")]
        public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetPriceHistoryByMetalSymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryByMetalSymbol(symbolRequest.Symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>GET /api/v1/history/{symbol}/latest — latest price history for one metal as <see cref="PriceHistoryDto"/>. Returns 404 if not found.</summary>
        [HttpGet("history/{symbol}/latest", Order = 5)]
        public async Task<ActionResult<PriceHistoryDto>> GetPriceHistoryByMetalSymbolLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var dto = await _apiService.GetPriceHistoryByMetalSymbolLatest(symbolRequest.Symbol.Trim(), cancellationToken);
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

        /// <summary>GET /api/v1/history/{symbol}/latest/trading — latest price as <see cref="TradingPriceDto"/> for TradingView (bid/ask, high/low, timestamps).</summary>
        [HttpGet("history/{symbol}/latest/trading", Order = 1)]
        public async Task<ActionResult<TradingPriceDto>> GetPriceHistoryTradingLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var dto = await _apiService.GetPriceHistoryTradingLatest(symbolRequest.Symbol.Trim(), cancellationToken);
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

        /// <summary>GET /api/v1/history/{symbol}/latest/karat — latest price as <see cref="KaratPricesDto"/> for KaratCalculatorView (price per gram 24k–10k).</summary>
        [HttpGet("history/{symbol}/latest/karat", Order = 1)]
        public async Task<ActionResult<KaratPricesDto>> GetPriceHistoryKaratLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var dto = await _apiService.GetPriceHistoryKaratLatest(symbolRequest.Symbol.Trim(), cancellationToken);
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

        /// <summary>GET /api/v1/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}/{firstDate}/{lastDate}", Order = 100)]
        [RequestTimeout("DataCruncher")]
        public async Task<IActionResult> GetPriceHistoryByMetalSymbolAndDateRange([FromRoute] SymbolRequest symbolRequest, [FromRoute] DateRangeRequest dateRangeRequest, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var start = DateOnly.Parse(dateRangeRequest.FirstDate);
            var end = DateOnly.Parse(dateRangeRequest.LastDate);
            var historyData = await _apiService.GetPriceHistoryByMetalSymbolAndDateRange(symbolRequest.Symbol.Trim(), start, end, ct);
            return Ok(historyData);
        }

        /// <summary>GET /api/v1/history/{symbol}/aggregated/{aggregation}/{count} — aggregated price history as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}/aggregated/{aggregation}/{count}", Order = 10)]
        [RequestTimeout("DataCruncher")]
        public async Task<IActionResult> GetPriceHistoryMetalData([FromRoute] SymbolRequest symbolRequest, [FromRoute] string aggregation, [FromRoute] int count, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryMetalData(symbolRequest.Symbol.Trim(), aggregation.Trim(), count, ct);
            return Ok(historyData);
        }
    }
}
