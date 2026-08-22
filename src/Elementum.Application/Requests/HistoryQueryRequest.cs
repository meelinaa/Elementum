namespace Elementum.Application.Requests;

/// <summary>
/// Validated query model for history ticks: currency, optional date window, and offset pagination.
/// </summary>
public record HistoryQueryRequest
{
    public string? Currency { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public int Skip { get; init; }
    public int? Take { get; init; }

    public string? NormalizedCurrency => CurrencyRequest.Normalize(Currency);
}
