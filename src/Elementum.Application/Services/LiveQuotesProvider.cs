using Elementum.Application.Exceptions;
using Elementum.Application.Logging;
using Elementum.Application.Models;
using Elementum.Application.Ports.Outbound;
using Microsoft.Extensions.Logging;

namespace Elementum.Application.Services;

/// <summary>
/// Default implementation of <see cref="ILiveQuotesProvider"/> fetching live metal quotes directly from API.
/// Caching is decoupled and handled via decorators in the Infrastructure layer.
/// Uses <see cref="LiveQuotesLogMessages"/> for zero-allocation logging and Exception Factories.
/// </summary>
public class LiveQuotesProvider : ILiveQuotesProvider
{
    private readonly IMetalsApiClient _apiClient;
    private readonly ILogger<LiveQuotesProvider> _logger;

    public LiveQuotesProvider(
        IMetalsApiClient apiClient,
        ILogger<LiveQuotesProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(logger);

        _apiClient = apiClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EdelmetalleApiResponse> GetLiveQuoteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var quote = await _apiClient.GetEdelmetallePricesAsync(cancellationToken);
            if (quote != null)
            {
                return quote;
            }
        }
        catch (Exception ex)
        {
            LiveQuotesLogMessages.FailedToFetchLiveQuotes(_logger, ex);
            throw;
        }

        throw ExternalApiException.EmptyLiveQuoteResponse();
    }
}
