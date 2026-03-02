using Elementum.Shared.Objects;
using Elementum_ServiceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Elementum_ServiceApi.Controllers
{
    [ApiController]
    [Route("api/v1")]
    public class FrontendService : ControllerBase
    {
        private readonly ApiService _apiService;

        public FrontendService(ApiService apiService)
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
        public async Task<ActionResult<Metals>> GetMetalBySymbol(string symbol, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "symbol", new[] { "Symbol is required and cannot be empty." } }
                }) { Title = "Invalid symbol", Status = StatusCodes.Status400BadRequest });
            }

            var metal = await _apiService.GetMetalBySymbol(symbol.Trim(), cancellationToken);
            if (metal == null)
            {
                return NotFound(new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Title = "Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No metal found with symbol '{symbol}'.",
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

        /// <summary>GET /api/history/{symbol} — price history for one metal. Returns JSON array.</summary>
        [HttpGet("history/{symbol}")]
        public async Task<ActionResult<IEnumerable<PriceHistory>>> GetPriceHistoryByMetalSymbol(string symbol, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "symbol", new[] { "Symbol is required and cannot be empty." } }
                }) { Title = "Invalid symbol", Status = StatusCodes.Status400BadRequest });
            }

            var historyData = await _apiService.GetPriceHistoryByMetalSymbol(symbol.Trim(), cancellationToken);
            return Ok(historyData);
        }

        /// <summary>
        /// GET /api/history/all/{firstDate}/{lastDate} — history in date range.
        /// Returns JSON array. Dates: yyyy-MM-dd.
        /// </summary>
        [HttpGet("history/all/{firstDate}/{lastDate}")]
        public async Task<IActionResult> GetPriceHistoryAllByDateRange(string firstDate, string lastDate, CancellationToken ct)
        {
            // ——— 1. ERROR HANDLING: Validate input ———
            if (!DateOnly.TryParse(firstDate, out var start) || !DateOnly.TryParse(lastDate, out var end))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "dates", new[] { "Invalid date format. Use yyyy-MM-dd." } }
                }) { Title = "Invalid date format", Status = StatusCodes.Status400BadRequest });
            }

            if (start > end)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "range", new[] { "Start date must not be after end date." } }
                }) { Title = "Invalid date range", Status = StatusCodes.Status400BadRequest });
            }

            var historyData = await _apiService.GetPriceHistoryAllByDateRange(start, end, ct);
            return Ok(historyData);
        }

        /// <summary>GET /api/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range. Dates: yyyy-MM-dd.</summary>
        [HttpGet("history/{symbol}/{firstDate}/{lastDate}")]
        public async Task<IActionResult> GetPriceHistoryByMetalSymbolAndDateRange(string symbol, string firstDate, string lastDate, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "symbol", new[] { "Symbol is required and cannot be empty." } }
                }) { Title = "Invalid symbol", Status = StatusCodes.Status400BadRequest });
            }

            if (!DateOnly.TryParse(firstDate, out var start) || !DateOnly.TryParse(lastDate, out var end))
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "dates", new[] { "Invalid date format. Use yyyy-MM-dd." } }
                }) { Title = "Invalid date format", Status = StatusCodes.Status400BadRequest });
            }

            if (start > end)
            {
                return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                {
                    { "range", new[] { "Start date must not be after end date." } }
                }) { Title = "Invalid date range", Status = StatusCodes.Status400BadRequest });
            }

            var historyData = await _apiService.GetPriceHistoryByMetalSymbolAndDateRange(symbol.Trim(), start, end, ct);
            return Ok(historyData);
        }
    }
}
