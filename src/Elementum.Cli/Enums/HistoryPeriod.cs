namespace Elementum.Cli.Enums;

/// <summary>
/// Aggregation for the history view. Passed to the API (aggregation + count).
/// API may implement e.g. GET history/{symbol}/aggregated?aggregation=weekly&amp;count=12.
/// </summary>
public enum HistoryPeriod
{
    Daily,
    Weekly,
    Monthly,
    Yearly
}
