using Microsoft.AspNetCore.Mvc;
using Elementum.Shared.Objects;
using Elementum_ServiceApi.Services;

namespace Elementum_ServiceApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class FrontendService : ControllerBase
    {
        private readonly ApiService _apiService;

        public FrontendService(ApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>GET /api/metals — all metals. Returns JSON array.</summary>
        [HttpGet("metals")]
        public async Task<IEnumerable<Metals>> GetAllMetals(CancellationToken cancellationToken)
        {
            return await _apiService.GetAllMetals(cancellationToken);
        }

        /// <summary>GET /api/metals/{symbol} — single metal by symbol. Returns JSON object.</summary>
        [HttpGet("metals/{symbol}")]
        public ActionResult<Metals> GetMetalBySymbol(string symbol)
        {
            // TODO: inject repository/service and return await _repo.GetMetalBySymbolAsync(symbol); return NotFound() if null
            return NotFound();
        }

        /// <summary>GET /api/history — all price history. Returns JSON array.</summary>
        [HttpGet("history")]
        public IEnumerable<PriceHistory> GetAllHistory()
        {
            // TODO: inject repository/service and return await _repo.GetAllHistoryAsync();
            return Array.Empty<PriceHistory>();
        }

        /// <summary>GET /api/history/{symbol} — price history for one metal. Returns JSON array.</summary>
        [HttpGet("history/{symbol}")]
        public IEnumerable<PriceHistory> GetHistoryBySymbol(string symbol)
        {
            // TODO: inject repository/service and return await _repo.GetHistoryBySymbolAsync(symbol);
            return Array.Empty<PriceHistory>();
        }

        /// <summary>GET /api/history/range/{firstDate}/{lastDate} — history in date range. Returns JSON array. Dates: yyyy-MM-dd.</summary>
        [HttpGet("history/range/{firstDate}/{lastDate}")]
        public IEnumerable<PriceHistory> GetHistoryByDateRange(string firstDate, string lastDate)
        {
            // TODO: parse firstDate/lastDate (e.g. DateOnly.Parse), inject repo, return await _repo.GetHistoryByDateRangeAsync(...);
            return Array.Empty<PriceHistory>();
        }

        /// <summary>GET /api/history/{symbol}/{firstDate}/{lastDate} — history for one metal in date range. Returns JSON array. Dates: yyyy-MM-dd.</summary>
        [HttpGet("history/{symbol}/{firstDate}/{lastDate}")]
        public IEnumerable<PriceHistory> GetHistoryBySymbolAndDateRange(string symbol, string firstDate, string lastDate)
        {
            // TODO: parse dates, inject repo, return await _repo.GetHistoryBySymbolAndDateRangeAsync(...);
            return Array.Empty<PriceHistory>();
        }
    }
}
