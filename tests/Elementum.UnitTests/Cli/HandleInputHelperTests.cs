using Elementum.Cli.Enums;
using Elementum.Cli.Input;

namespace Elementum.Cli.Tests;

public class HandleInputHelperTests
{
    // [R]IGHT-BICEP: numeric metal key selects the metal and triggers a render callback
    [Fact]
    public void HandleInputWithMetals_WhenNoMetalSelected_SetsMetalAndInvokesRender()
    {
        // Arrange
        var app = new Elementum.Cli.AppContext { State = AppState.Detail };
        Elementum.Cli.AppContext.Current = app;
        app.CurrentSelectedMetal = null;

        var renderCalls = 0;
        Task Render()
        {
            renderCalls++;
            return Task.CompletedTask;
        }

        var key = new ConsoleKeyInfo('1', ConsoleKey.D1, shift: false, alt: false, control: false);

        // Act
        HandleInputHelper.HandleInputWithMetals(key, Render);

        // Assert
        Assert.Equal(Metall.Gold, app.CurrentSelectedMetal);
        Assert.Equal(1, renderCalls);
    }

    // [E]RROR: unknown keys leave selection unchanged and skip rendering
    [Fact]
    public void HandleInputWithMetals_WhenUnknownKey_DoesNotSetMetalOrInvokeRender()
    {
        // Arrange
        var app = new Elementum.Cli.AppContext { State = AppState.Detail };
        Elementum.Cli.AppContext.Current = app;
        app.CurrentSelectedMetal = null;

        var renderCalls = 0;
        Task Render()
        {
            renderCalls++;
            return Task.CompletedTask;
        }

        var key = new ConsoleKeyInfo('x', ConsoleKey.X, shift: false, alt: false, control: false);

        // Act
        HandleInputHelper.HandleInputWithMetals(key, Render);

        // Assert
        Assert.Null(app.CurrentSelectedMetal);
        Assert.Equal(0, renderCalls);
    }
}
