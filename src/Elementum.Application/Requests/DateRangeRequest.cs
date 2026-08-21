namespace Elementum.Application.Requests;

/// <summary>
/// Optional inclusive date window for history queries (<c>from</c>/<c>to</c>, ISO <c>yyyy-MM-dd</c>).
/// Omitted bounds default to the last 30 days in the use case.
/// </summary>
public record DateRangeRequest
{
    public string? From { get; init; }
    public string? To { get; init; }

    public DateRangeRequest() { }

    public DateRangeRequest(string? from, string? to)
    {
        From = from;
        To = to;
    }
}
