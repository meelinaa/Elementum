using Elementum.Cli.Input;
using Elementum.Cli.Rendering;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>
/// Detail view displaying static system architecture, functionality guide, and project composition.
/// </summary>
public class InfoView : IDetailView
{
    /// <summary>
    /// Renders the informational overview screen containing architecture, features, and project layers.
    /// </summary>
    /// <returns>A completed task representing the asynchronous render operation.</returns>
    public Task RenderAsync()
    {
        InfoRenderer.RenderInfo();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Processes keyboard input when viewing the Info screen (e.g. Escape/Arrow navigation).
    /// </summary>
    /// <param name="key">The key pressed by the user.</param>
    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
