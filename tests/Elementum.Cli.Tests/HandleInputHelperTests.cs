using Elementum_Cli.Enums;
using Elementum_Cli.Input;

namespace Elementum.Cli.Tests;

public class HandleInputHelperTests
{
    [Fact]
    public void HandleInputWithMetals_WhenNoMetalSelected_SetsMetalAndInvokesRender()
    {
        var app = new Elementum_Cli.AppContext { State = AppState.Detail };
        Elementum_Cli.AppContext.Current = app;
        app.CurrentSelectedMetal = null;

        var renderCalls = 0;
        Task Render()
        {
            renderCalls++;
            return Task.CompletedTask;
        }

        var key = new ConsoleKeyInfo('1', ConsoleKey.D1, shift: false, alt: false, control: false);
        HandleInputHelper.HandleInputWithMetals(key, Render);

        Assert.Equal(Metall.Gold, app.CurrentSelectedMetal);
        Assert.Equal(1, renderCalls);
    }

    [Fact]
    public void HandleInputWithMetals_WhenUnknownKey_DoesNotSetMetalOrInvokeRender()
    {
        var app = new Elementum_Cli.AppContext { State = AppState.Detail };
        Elementum_Cli.AppContext.Current = app;
        app.CurrentSelectedMetal = null;

        var renderCalls = 0;
        Task Render()
        {
            renderCalls++;
            return Task.CompletedTask;
        }

        var key = new ConsoleKeyInfo('x', ConsoleKey.X, shift: false, alt: false, control: false);
        HandleInputHelper.HandleInputWithMetals(key, Render);

        Assert.Null(app.CurrentSelectedMetal);
        Assert.Equal(0, renderCalls);
    }
}

