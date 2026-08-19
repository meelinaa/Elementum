using Elementum.Cli.Enums;
using Elementum.Cli.Views;

namespace Elementum.Cli.Tests;

public class ViewRegistryTests
{
    // [R]IGHT-BICEP: repeated lookups return the same view instance (registry singleton per view)
    [Fact]
    public void Get_ReturnsSameInstance_ForSameView()
    {
        // Act
        var v1 = ViewRegistry.Get(DetailView.Dashboard);
        var v2 = ViewRegistry.Get(DetailView.Dashboard);

        // Assert
        Assert.Same(v1, v2);
    }

    // [R]IGHT-BICEP: different views resolve to distinct instances
    [Fact]
    public void Get_ReturnsDifferentInstances_ForDifferentViews()
    {
        // Act
        var dashboard = ViewRegistry.Get(DetailView.Dashboard);
        var trading = ViewRegistry.Get(DetailView.TradingView);

        // Assert
        Assert.NotSame(dashboard, trading);
    }
}
