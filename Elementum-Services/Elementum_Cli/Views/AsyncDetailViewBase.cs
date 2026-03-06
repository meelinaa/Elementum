using Elementum_Cli.Helper;

namespace Elementum_Cli.Views;

/// <summary>
/// Base class for detail views that load and display data without metal selection (e.g. Dashboard, List of Metals).
/// Shared flow: Clear → header + loading message + footer → LoadAndRenderAsync().
/// </summary>
public abstract class AsyncDetailViewBase : IDetailView
{
    /// <summary>Title shown in the view header, e.g. "CURRENT MARKET OVERVIEW".</summary>
    protected abstract string ViewTitle { get; }

    /// <summary>Message shown while data is loading, e.g. "Loading market data…".</summary>
    protected abstract string LoadingMessage { get; }

    public async Task RenderAsync()
    {
        Console.Clear();
        CliOutputHelper.RenderViewHeader(ViewTitle);
        Console.WriteLine($"  {LoadingMessage}");
        CliOutputHelper.RenderViewFooter();

        await LoadAndRenderAsync();
    }

    /// <summary>Load data and render the view content. Called after the common header/loading/footer.</summary>
    protected abstract Task LoadAndRenderAsync();

    public virtual void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
