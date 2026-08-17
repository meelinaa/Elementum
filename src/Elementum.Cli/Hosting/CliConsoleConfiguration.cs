using System.Text;

namespace Elementum.Cli.Hosting;

/// <summary>
/// Console host settings for the terminal UI (encoding and cursor visibility).
/// </summary>
public static class CliConsoleConfiguration
{
    /// <summary>
    /// Uses UTF-8 so box-drawing and metal symbols render correctly in modern terminals.
    /// Hides the cursor so the menu selection arrow does not flicker over the list.
    /// </summary>
    public static void Apply()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
    }
}
