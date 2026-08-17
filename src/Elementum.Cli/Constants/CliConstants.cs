namespace Elementum_Cli.Constants;

/// <summary>
/// Central place for CLI layout and table constants. Improves maintainability and consistency.
/// </summary>
public static class CliConstants
{
    /// <summary>Total width of the main view area (headers, footer, section boxes).</summary>
    public const int ViewWidth = 72;

    /// <summary>Pad width for main menu titles (before <c> - </c>) so hyphens align.</summary>
    public const int MenuMainTitleColumnWidth = 12;

    /// <summary>User-visible date format for <see cref="DateOnly"/> / date parts (not API/DB wire format).</summary>
    public const string DisplayDateFormat = "dd.MM.yyyy";

    /// <summary>User-visible UTC date+time for timestamps in detail views.</summary>
    public const string DisplayDateTimeUtcFormat = "dd.MM.yyyy HH:mm";

    /// <summary>Prefix for the dashboard table (current market overview).</summary>
    public const string DashboardTablePrefix = "   ";

    /// <summary>Column widths for dashboard: ID, NAME, EXCHANGE, PRICE USD, CHANGES (Chp).</summary>
    public static readonly int[] DashboardColumnWidths = { 4, 9, 10, 12, 14 };

    /// <summary>Prefix for the metal list table (list of all metals).</summary>
    public const string ListMetallTablePrefix = "  ";

    /// <summary>Column widths for metal list: ID, SYMBOL, NAME.</summary>
    public static readonly int[] ListMetallColumnWidths = { 6, 10, 22 };
}
