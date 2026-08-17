using Elementum.Cli.Constants;
using Elementum.Cli.Input;
using Elementum.Cli.Output;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>
/// Static info screen: app title, section title, and GoldAPI.com data policy (no API call).
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
        Console.WriteLine(CliStrings.InfoGoldapiDailyLine);
        Console.WriteLine(CliStrings.InfoGoldapiQuotaLine);

        CliOutputHelper.RenderViewFooter();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void HandleInput(ConsoleKeyInfo key)
    {
        HandleInputHelper.HandleInput(key);
    }
}
