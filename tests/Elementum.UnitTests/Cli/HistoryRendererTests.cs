using Elementum.Cli.Constants;
using Elementum.Cli.Rendering;

namespace Elementum.Cli.Tests;

public class HistoryRendererTests
{
    [Fact]
    public void ComputeHistoryViewWidth_ScalesWithEntryCount_AndNotBelowDefault()
    {
        Assert.Equal(CliConstants.ViewWidth, HistoryRenderer.ComputeHistoryViewWidth(0));
        int w31 = HistoryRenderer.ComputeHistoryViewWidth(31);
        Assert.True(w31 > CliConstants.ViewWidth);
        Assert.Equal(2 + 6 + 3 + 16 + 5 + 31 * 2, w31);
    }

    [Fact]
    public void BuildSparkline_MapsPositiveNegativeZero()
    {
        var spark = HistoryRenderer.BuildSparkline(new[] { 1.0, 0.0, -1.0 });
        Assert.Equal("█─▁", spark);
    }

    [Fact]
    public void BuildSparkline_DoubleWidth_DuplicatesEachBlock()
    {
        var spark = HistoryRenderer.BuildSparkline(new[] { 1.0, 0.0, -1.0 }, doubleWidth: true);
        Assert.Equal("██──▁▁", spark);
    }

    [Fact]
    public void BuildSparkline_Empty_ReturnsEmptyString()
    {
        var spark = HistoryRenderer.BuildSparkline(Array.Empty<double>());
        Assert.Equal("", spark);
    }
}

