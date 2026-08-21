namespace Elementum.Application.Requests;

/// <summary>
/// Offset pagination for history ticks. <c>take</c> is optional; the use case applies default 500 and a hard cap of 2000.
/// </summary>
public record HistoryPageRequest
{
    public int Skip { get; init; }
    public int? Take { get; init; }
}
