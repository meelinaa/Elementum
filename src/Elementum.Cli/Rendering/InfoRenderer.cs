using Elementum.Cli.Constants;
using Elementum.Cli.Output;

namespace Elementum.Cli.Rendering;

/// <summary>
/// Dedicated renderer for the CLI Info view: formats and prints system architecture,
/// core functionalities, and solution project overview in structured ASCII frames.
/// </summary>
public static class InfoRenderer
{
    private const int InnerContentWidth = 64;
    private const int BorderWidth = 66;

    /// <summary>
    /// Renders the complete Info view including project overview, feature breakdowns,
    /// and clean architecture project descriptions into the console.
    /// </summary>
    public static void RenderInfo()
    {
        CliOutputHelper.SafeClear();
        CliOutputHelper.RenderViewHeader("INFO — SYSTEM ARCHITECTURE & FEATURES");

        // Section 1: About Elementum
        CliOutputHelper.RenderSectionTitle("About Elementum");
        WriteBoxLine("Elementum is a production-ready precious metals tracking &");
        WriteBoxLine("technical analysis platform designed with Clean Architecture");
        WriteBoxLine("and Hexagonal Ports/Adapters patterns in .NET 10.");
        WriteBoxLine("");
        WriteBoxLine("Supported assets: Gold (XAU), Silver (XAG), Platinum (XPT),");
        WriteBoxLine("and Palladium (XPD) with live spot quotes and indicators.");
        WriteBoxBottom();

        CliOutputHelper.RenderViewFooter();
    }

    /// <summary>
    /// Writes a single padded line enclosed in box drawing borders.
    /// </summary>
    /// <param name="text">The text content to render within the box frame.</param>
    private static void WriteBoxLine(string text)
    {
        Console.WriteLine($"  │ {text.PadRight(InnerContentWidth)} │");
    }

    /// <summary>
    /// Writes the closing bottom border for a section box.
    /// </summary>
    private static void WriteBoxBottom()
    {
        Console.WriteLine($"  └{new string('─', BorderWidth)}┘");
    }
}
