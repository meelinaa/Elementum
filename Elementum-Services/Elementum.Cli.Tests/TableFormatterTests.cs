using Elementum_Cli.Helper;

namespace Elementum.Cli.Tests;

public class TableFormatterTests
{
    [Fact]
    public void Borders_HaveSameLengthAsRow_ForGivenWidths()
    {
        var prefix = "  ";
        var widths = new[] { 4, 9, 10 };

        var top = TableFormatter.BuildTopBorder(prefix, widths);
        var mid = TableFormatter.BuildMidBorder(prefix, widths);
        var bottom = TableFormatter.BuildBottomBorder(prefix, widths);
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "ID", "SYMBOL", "NAME" });

        Assert.Equal(top.Length, row.Length);
        Assert.Equal(mid.Length, row.Length);
        Assert.Equal(bottom.Length, row.Length);
    }

    [Fact]
    public void BuildRow_PadsCellsToColumnWidths()
    {
        var prefix = "";
        var widths = new[] { 4, 6 };
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "A", "B" });

        // "│ " + A padded to 4 + " │ " + B padded to 6 + " │"
        Assert.Contains("│ A    │ B      │", row);
    }

    [Fact]
    public void BuildEmptyRow_SpansAllColumns()
    {
        var prefix = " ";
        var widths = new[] { 2, 3, 4 };
        var msg = "No data";
        var empty = TableFormatter.BuildEmptyRow(prefix, widths, msg);

        Assert.StartsWith(prefix + "│ ", empty);
        Assert.EndsWith(" │", empty);
        Assert.Contains(msg, empty);

        // Empty row should match regular row length (with same prefix/widths).
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "1", "2", "3" });
        Assert.Equal(row.Length, empty.Length);
    }
}

