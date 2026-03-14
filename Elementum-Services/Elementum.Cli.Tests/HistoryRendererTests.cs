using Elementum_Cli.Rendering;

namespace Elementum.Cli.Tests;

public class HistoryRendererTests
{
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

