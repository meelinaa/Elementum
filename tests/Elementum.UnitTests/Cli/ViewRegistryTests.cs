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

    // RIGHT-BIC[E]P: Constructor throws ArgumentNullException when api dependency is null
    [Fact]
    public void Constructor_WhenApiNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ViewRegistry(null!));
    }

    // [R]IGHT-BICEP: RequiresMetalSelection returns true only for views that operate on a specific metal
    [Theory]
    [InlineData(DetailView.TradingView, true)]
    [InlineData(DetailView.History, true)]
    [InlineData(DetailView.Dashboard, false)]
    [InlineData(DetailView.Info, false)]
    public void RequiresMetalSelection_ReturnsExpectedBoolean(DetailView view, bool expectedRequires)
    {
        // Act
        var result = ViewRegistry.RequiresMetalSelection(view);

        // Assert
        Assert.Equal(expectedRequires, result);
    }

    // RIGHT-[B]ICEP: undefined DetailView value falls back to DashboardView without throwing
    [Fact]
    public void Get_WhenUndefinedDetailView_FallsBackToDashboardView()
    {
        var sut = CreateSut();

        // Act
        var view = sut.Get((DetailView)999);

        // Assert
        Assert.IsType<DashboardView>(view);
    }
}
