using Elementum.Cli.Constants;
using Elementum.Cli.Rendering;

namespace Elementum.Cli.Tests;

public class HistoryRendererTests
{
    // RIGHT-[B]ICEP: view width stays at default for zero entries and scales with entry count
    [Fact]
    public void ComputeHistoryViewWidth_ScalesWithEntryCount_AndNotBelowDefault()
    {
        // Act
        var widthForZero = HistoryRenderer.ComputeHistoryViewWidth(0);
        var widthFor31 = HistoryRenderer.ComputeHistoryViewWidth(31);

        // Assert
        Assert.Equal(CliConstants.ViewWidth, widthForZero);
        Assert.True(widthFor31 > CliConstants.ViewWidth);
        Assert.Equal(2 + 6 + 3 + 16 + 5 + 31 * 2, widthFor31);
    }

    // [R]IGHT-BICEP: positive, zero, and negative values map to expected sparkline blocks
    [Fact]
    public void BuildSparkline_MapsPositiveNegativeZero()
    {
        // Act
        var spark = HistoryRenderer.BuildSparkline(new[] { 1.0, 0.0, -1.0 });

        // Assert
        Assert.Equal("█─▁", spark);
    }

    // [R]IGHT-BICEP: double-width mode duplicates each sparkline block
    [Fact]
    public void BuildSparkline_DoubleWidth_DuplicatesEachBlock()
    {
        // Act
        var spark = HistoryRenderer.BuildSparkline(new[] { 1.0, 0.0, -1.0 }, doubleWidth: true);

        // Assert
        Assert.Equal("██──▁▁", spark);
    }

    // RIGHT-BIC[E]P: empty input yields an empty sparkline instead of throwing
    [Fact]
    public void BuildSparkline_Empty_ReturnsEmptyString()
    {
        // Act
        var spark = HistoryRenderer.BuildSparkline(Array.Empty<double>());

        // Assert
        Assert.Equal("", spark);
    }
}
