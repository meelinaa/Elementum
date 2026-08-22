namespace Elementum.Cli.Constants;

/// <summary>
/// Central place for CLI layout and table constants.
/// </summary>
public static class CliConstants
{
    /// <summary>Total width of the main view area (headers, footer, section boxes).</summary>
    public const int ViewWidth = 72;

    /// <summary>Pad width for main menu titles (before <c> - </c>) so hyphens align.</summary>
    public const int MenuMainTitleColumnWidth = 12;

    /// <summary>User-visible date format for <see cref="DateOnly"/> / date parts.</summary>
    public const string DisplayDateFormat = "dd.MM.yyyy";

    /// <summary>User-visible local date+time format for timestamps in detail views.</summary>
    public const string DisplayDateTimeFormat = "dd.MM.yyyy HH:mm:ss";

    /// <summary>Prefix for the dashboard table (current market overview).</summary>
    public const string DashboardTablePrefix = "  ";

    /// <summary>Column widths for dashboard: ID, NAME, PRICE USD, PRICE EUR, CHANGES (Chp).</summary>
    public static readonly int[] DashboardColumnWidths = { 4, 10, 13, 13, 14 };
}
