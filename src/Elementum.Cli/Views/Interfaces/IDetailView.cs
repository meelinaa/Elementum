namespace Elementum_Cli.Views.Interfaces;

/// <summary>
/// Contract for a detail view (Dashboard, Trading, History, etc.). Each view can render asynchronously and handle keyboard input.
/// </summary>
public interface IDetailView
{
    /// <summary>Renders the view content (header, data, footer). May load data from the API.</summary>
    Task RenderAsync();

    /// <summary>Handles a single key press (e.g. ESC to go back, 1–3 for metal selection).</summary>
    void HandleInput(ConsoleKeyInfo key);
}
