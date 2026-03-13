using Elementum_Cli.Input;
using Elementum_Cli.Output;
using Elementum_Cli.Views.Interfaces;

namespace Elementum_Cli.Views;

/// <summary>
/// Base class for detail views that require a metal selection (e.g. Trading, Karat).
/// Shared flow: Clear → prompt if no metal → header + loading message + footer → LoadAndRenderAsync(sym, name).
/// </summary>
public abstract class MetalDetailViewBase : IDetailView
{
    /// <summary>Title shown in the view header, e.g. "TRADING & DAILY ANALYSIS".</summary>
    protected abstract string ViewTitle { get; }

    /// <summary>Message shown while data is loading, e.g. "Loading daily data…".</summary>
    protected abstract string LoadingMessage { get; }

    public async Task RenderAsync()
    {
        var app = AppContext.Current!;
        Console.Clear();
        if (!app.CurrentSelectedMetal.HasValue)
        {
            CliOutputHelper.RenderMetalSelectionPrompt();
            return;
        }

        var sym = MetallHelper.GetSymbol(app.CurrentSelectedMetal.Value);
        var name = MetallHelper.GetName(app.CurrentSelectedMetal.Value);

        CliOutputHelper.RenderViewHeader($"{ViewTitle} — {name.ToUpperInvariant()} ({sym})");
        Console.WriteLine($"  {LoadingMessage}");
        CliOutputHelper.RenderViewFooter();

        await LoadAndRenderAsync(sym, name);
    }

    /// <summary>Load data and render the view content for the given metal. Called after the common header/loading/footer.</summary>
    protected abstract Task LoadAndRenderAsync(string sym, string name);

    public virtual void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInputWithMetals(key, () => RenderAsync());
    }
}
