using Elementum.Cli.Api;
using Elementum.Cli.Enums;
using Elementum.Cli.Views;
using Moq;

namespace Elementum.Cli.Tests;

public class ViewRegistryTests
{
    private static ViewRegistry CreateSut() => new(Mock.Of<IHttpCall>());

    // [R]IGHT-BICEP: repeated lookups return the same view instance (registry singleton per view)
    [Fact]
    public void Get_ReturnsSameInstance_ForSameView()
    {
        var sut = CreateSut();

        // Act
        var v1 = sut.Get(DetailView.Dashboard);
        var v2 = sut.Get(DetailView.Dashboard);

        // Assert
        Assert.Same(v1, v2);
    }

    // [R]IGHT-BICEP: different views resolve to distinct instances
    [Fact]
    public void Get_ReturnsDifferentInstances_ForDifferentViews()
    {
        var sut = CreateSut();

        // Act
        var dashboard = sut.Get(DetailView.Dashboard);
        var trading = sut.Get(DetailView.TradingView);

        // Assert
        Assert.NotSame(dashboard, trading);
    }
}
