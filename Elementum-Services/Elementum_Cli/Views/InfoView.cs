using Elementum_Cli.Helper;

namespace Elementum_Cli.Views;

public class InfoView : IDetailView
{
    public Task RenderAsync()
    {
        Console.Clear();

        CliOutputHelper.RenderViewHeader(CliStrings.InfoHeader);

        CliOutputHelper.RenderSectionTitle(CliStrings.InfoSectionTitle);
        Console.WriteLine(CliStrings.InfoAppLine);
        Console.WriteLine(CliStrings.InfoDescriptionLine);
        Console.WriteLine(CliStrings.InfoBoxBottom);
        Console.WriteLine();
        Console.WriteLine(CliStrings.InfoBackendPlaceholder);
        Console.WriteLine();
        Console.WriteLine(CliStrings.InfoDummyOutput);

        CliOutputHelper.RenderViewFooter();
        return Task.CompletedTask;
    }

    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
