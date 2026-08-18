using Elementum.Domain.Models;

namespace Elementum.Application.Services;

/// <summary>
/// Service responsible for fetching, caching, and providing live metal quotes.
/// </summary>
public interface ILiveQuotesProvider
{
    /// <summary>
    /// Returns the latest live quote (from 5-minute memory cache, remote API, or fallback).
    /// </summary>
    Task<EdelmetalleApiResponse> GetLiveQuoteAsync(CancellationToken cancellationToken = default);
}
