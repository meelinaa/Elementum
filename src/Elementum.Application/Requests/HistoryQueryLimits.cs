using System.Globalization;

namespace Elementum.Application.Requests;

/// <summary>
/// Server-side bounds for history queries. <c>take</c> is clamped to <see cref="MaxTake"/> even if the client asks for more.
/// </summary>
public static class HistoryQueryLimits
{
    public const int DefaultTake = 500;
    public const int MaxTake = 2000;
    public const int DefaultLookbackDays = 30;
    public const string DateFormat = "yyyy-MM-dd";

    public static int ClampTake(int? take)
    {
        if (take is null or <= 0)
            return DefaultTake;

        return Math.Min(take.Value, MaxTake);
    }

    public static int ClampSkip(int skip) => skip < 0 ? 0 : skip;

    /// <summary>
    /// Resolves optional ISO dates: omitted range defaults to the last <see cref="DefaultLookbackDays"/> days;
    /// a single bound fills the other (from → today, to → to minus lookback).
    /// </summary>
    public static (DateOnly From, DateOnly To) ResolveRange(string? from, string? to, DateOnly today)
    {
        var parsedFrom = TryParse(from);
        var parsedTo = TryParse(to);

        if (parsedFrom is null && parsedTo is null)
            return (today.AddDays(-DefaultLookbackDays), today);

        if (parsedFrom is null)
            return (parsedTo!.Value.AddDays(-DefaultLookbackDays), parsedTo.Value);

        if (parsedTo is null)
            return (parsedFrom.Value, today);

        return (parsedFrom.Value, parsedTo.Value);
    }

    private static DateOnly? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateOnly.TryParseExact(
                value.Trim(),
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        return null;
    }
}
