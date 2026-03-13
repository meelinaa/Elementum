using Elementum.Shared.DTOs;
using Elementum.Shared.Objects;
using Elementum_ServiceApi.Models;
using Elementum_ServiceApi.Services.Interfaces;
using Microsoft.AspNetCore.Http.Timeouts;
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

        /// <summary>GET /api/v1/metals/all — all metals as <see cref="MetalsDto"/> (Id, Symbol, Name).</summary>
        [HttpGet("metals/all")]
        public async Task<IEnumerable<MetalsDto>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _apiService.GetAllMetals(cancellationToken);
        }

        /// <summary>GET /api/v1/metals/{symbol} — single metal by symbol as <see cref="MetalsDto"/>. Returns 404 if not found.</summary>
        [HttpGet("metals/{symbol}")]
        public async Task<ActionResult<MetalsDto>> GetMetalBySymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
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
        [RequestTimeout("DataCruncher")]
        public async Task<IEnumerable<PriceHistory>> GetPriceHistoryAll(CancellationToken cancellationToken)
        {
            return await _apiService.GetPriceHistoryAll(cancellationToken);
        }

        /// <summary>GET /api/v1/history/all/latest — latest per metal as <see cref="PriceHistoryDto"/> for Dashboard.</summary>
        [HttpGet("history/all/latest")]
        public async Task<IEnumerable<PriceHistoryDto>> GetPriceHistoryAllLatest(CancellationToken cancellationToken)
        {
            return await _apiService.GetPriceHistoryAllLatest(cancellationToken);
        }

        /// <summary>GET /api/v1/history/{symbol} — price history for one metal as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}")]
        [RequestTimeout("DataCruncher")]
        public async Task<ActionResult<IEnumerable<PriceHistoryDto>>> GetPriceHistoryByMetalSymbol([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryByMetalSymbol(symbolRequest.Symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>GET /api/history/{symbol}/latest — latest price history for one metal.</summary>
        [HttpGet("history/{symbol}/latest")]
        public async Task<ActionResult<IEnumerable<PriceHistory>>> GetPriceHistoryByMetalSymbolLatest([FromRoute] SymbolRequest symbolRequest, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryByMetalSymbolLatest(symbolRequest.Symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>GET /api/v1/history/{symbol}/latest/trading — latest price as <see cref="TradingPriceDto"/> for TradingView (bid/ask, high/low, timestamps).</summary>
        [HttpGet("history/{symbol}/latest/trading")]
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
        [HttpGet("history/{symbol}/latest/karat")]
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

        /// <summary>GET /api/v1/history/all/{firstDate}/{lastDate} — history in date range as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/all/{firstDate}/{lastDate}")]
        [RequestTimeout("DataCruncher")]
        public async Task<IActionResult> GetPriceHistoryAllByDateRange([FromRoute] DateRangeRequest request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var start = DateOnly.Parse(request.FirstDate);
            var end = DateOnly.Parse(request.LastDate);
            var historyData = await _apiService.GetPriceHistoryAllByDateRange(start, end, ct);
            return Ok(historyData);
        }

        /// <summary>GET /api/v1/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}/{firstDate}/{lastDate}")]
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

        /// <summary>GET /api/v1/history/{symbol}/aggregated=... — aggregated price history as <see cref="PriceHistoryDto"/> array.</summary>
        [HttpGet("history/{symbol}/aggregated={aggregation}&{count}")]
        [RequestTimeout("DataCruncher")]
        public async Task<IActionResult> GetPriceHistoryMetalData([FromRoute] SymbolRequest symbolRequest, [FromRoute] AggregationRequest aggregationRequest, CancellationToken ct)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var historyData = await _apiService.GetPriceHistoryMetalData(symbolRequest.Symbol.Trim(), aggregationRequest.Aggregation.Trim(), aggregationRequest.Count, ct);
            return Ok(historyData);
        }
    }
}
