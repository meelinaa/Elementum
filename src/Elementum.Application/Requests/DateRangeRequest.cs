namespace Elementum.Application.Requests;

/// <summary>
/// Request parameter model for queries filtering by date range.
/// </summary>
public record DateRangeRequest
{
    public string FirstDate { get; init; } = string.Empty;
    public string LastDate { get; init; } = string.Empty;

    public DateRangeRequest() { }
    public DateRangeRequest(string firstDate, string lastDate)
    {
        FirstDate = firstDate;
        LastDate = lastDate;
    }
}
