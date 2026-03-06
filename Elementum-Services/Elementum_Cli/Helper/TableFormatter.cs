namespace Elementum_Cli.Helper;

/// <summary>
/// Builds consistent table borders and data rows from column widths.
/// Border segment length is width+2 so that borders align with rows (row uses " │ " between cells).
/// </summary>
public static class TableFormatter
{
    /// <summary>Builds top border: prefix + ┌─…─┬─…─┐ (segment length = width+2 to match row length).</summary>
    public static string BuildTopBorder(string prefix, params int[] widths)
        => prefix + "┌" + string.Join("┬", widths.Select(w => new string('─', w + 2))) + "┐";

    /// <summary>Builds middle border: prefix + ├─…─┼─…─┤.</summary>
    public static string BuildMidBorder(string prefix, params int[] widths)
        => prefix + "├" + string.Join("┼", widths.Select(w => new string('─', w + 2))) + "┤";

    /// <summary>Builds bottom border: prefix + └─…─┴─…─┘.</summary>
    public static string BuildBottomBorder(string prefix, params int[] widths)
        => prefix + "└" + string.Join("┴", widths.Select(w => new string('─', w + 2))) + "┘";

    /// <summary>Builds a data row. RightAlign: true = PadLeft for that column (e.g. numbers). Null = all left.</summary>
    public static string BuildRow(string prefix, int[] widths, string[] cells, bool[]? rightAlign = null)
    {
        rightAlign ??= new bool[widths.Length];
        var parts = new List<string>();
        for (int i = 0; i < widths.Length; i++)
        {
            string cell = i < cells.Length ? cells[i] : "";
            string padded = rightAlign[i] ? cell.PadLeft(widths[i]) : cell.PadRight(widths[i]);
            parts.Add(padded);
        }
        return prefix + "│ " + string.Join(" │ ", parts) + " │";
    }

    /// <summary>Builds a single-cell row spanning all columns (e.g. "No metals found.").</summary>
    public static string BuildEmptyRow(string prefix, int[] widths, string message)
    {
        // Same total content length as a normal row: 2 + sum(widths) + 3*(n-1) + 2 - 4 = sum(widths) + 3*n - 3
        int contentLen = widths.Sum() + 3 * widths.Length - 3;
        return prefix + "│ " + message.PadRight(contentLen) + " │";
    }
}
