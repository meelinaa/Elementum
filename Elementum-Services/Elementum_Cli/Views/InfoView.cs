using Elementum_Cli.Helper;

namespace Elementum_Cli.Views;

public class InfoView : IDetailView
{
    public Task RenderAsync()
    {
        Console.Clear();

        CliOutputHelper.RenderViewHeader("INFO — ELEMENTUM TERMINAL");

        CliOutputHelper.RenderSectionTitle("Master data & creation date (CreatedAt)");
        Console.WriteLine("  │  ELEMENTUM TERMINAL  v1.1                                    │");
        Console.WriteLine("  │  Master data & creation date – overview.                     │");
        Console.WriteLine("  └──────────────────────────────────────────────────────────────┘");
        Console.WriteLine();
        Console.WriteLine("  (Backend logic to follow)");
        Console.WriteLine();
        Console.WriteLine("  Dummy output (no HTTP call)");

        CliOutputHelper.RenderViewFooter();
        return Task.CompletedTask;
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
