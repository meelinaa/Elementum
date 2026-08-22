using Elementum.Cli.Output;

namespace Elementum.Cli.Tests;

public class TableFormatterTests
{
    // [R]IGHT-BICEP: border segments align with row width for consistent table layout
    [Fact]
    public void Borders_HaveSameLengthAsRow_ForGivenWidths()
    {
        // Arrange
        var prefix = "  ";
        var widths = new[] { 4, 9, 10 };

        // Act
        var top = TableFormatter.BuildTopBorder(prefix, widths);
        var mid = TableFormatter.BuildMidBorder(prefix, widths);
        var bottom = TableFormatter.BuildBottomBorder(prefix, widths);
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "ID", "SYMBOL", "NAME" });

        // Assert
        Assert.Equal(top.Length, row.Length);
        Assert.Equal(mid.Length, row.Length);
        Assert.Equal(bottom.Length, row.Length);
    }

    // [R]IGHT-BICEP: cells are padded to declared column widths
    [Fact]
    public void BuildRow_PadsCellsToColumnWidths()
    {
        // Arrange
        var prefix = "";
        var widths = new[] { 4, 6 };

        // Act
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "A", "B" });

        // Assert
        Assert.Contains("│ A    │ B      │", row);
    }

    // RIGHT-[B]ICEP: empty-state row spans all columns and matches a regular row length
    [Fact]
    public void BuildEmptyRow_SpansAllColumns()
    {
        // Arrange
        var prefix = " ";
        var widths = new[] { 2, 3, 4 };
        var msg = "No data";

        // Act
        var empty = TableFormatter.BuildEmptyRow(prefix, widths, msg);
        var row = TableFormatter.BuildRow(prefix, widths, new[] { "1", "2", "3" });

        // Assert
        Assert.StartsWith(prefix + "│ ", empty);
        Assert.EndsWith(" │", empty);
        Assert.Contains(msg, empty);
        Assert.Equal(row.Length, empty.Length);
    }
}
