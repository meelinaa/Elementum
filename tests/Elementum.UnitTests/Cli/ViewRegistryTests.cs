using Elementum.Cli.Enums;
using Elementum.Cli.Views;

namespace Elementum.Cli.Tests;

public class ViewRegistryTests
{
    [Fact]
    public void Get_ReturnsSameInstance_ForSameView()
    {
        var v1 = ViewRegistry.Get(DetailView.Dashboard);
        var v2 = ViewRegistry.Get(DetailView.Dashboard);

        Assert.Same(v1, v2);
    }

    [Fact]
    public void Get_ReturnsDifferentInstances_ForDifferentViews()
    {
        var dashboard = ViewRegistry.Get(DetailView.Dashboard);
        var list = ViewRegistry.Get(DetailView.ListMetals);

        Assert.NotSame(dashboard, list);
    }
}

