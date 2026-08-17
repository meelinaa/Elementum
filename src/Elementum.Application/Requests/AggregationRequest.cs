namespace Elementum.Application.Requests;

/// <summary>
/// Request model for aggregated price queries (interval, optional date bounds).
/// </summary>
public record AggregationRequest
{
    public string? FirstDate { get; init; }
    public string? LastDate { get; init; }
    public string? Interval { get; init; }

    public AggregationRequest() { }
    public AggregationRequest(string? firstDate, string? lastDate, string? interval)
    {
        FirstDate = firstDate;
        LastDate = lastDate;
        Interval = interval;
    }
}
