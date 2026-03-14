using Elementum_Cli.Constants;
using Elementum_Cli.Input;
using Elementum_Cli.Output;
using Elementum_Cli.Views.Interfaces;

namespace Elementum_Cli.Views;

/// <summary>
/// Static info screen: app title, section title, and placeholder text (no API call).
/// </summary>
public class InfoView : IDetailView
{
    /// <inheritdoc />
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

    /// <inheritdoc />
    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
