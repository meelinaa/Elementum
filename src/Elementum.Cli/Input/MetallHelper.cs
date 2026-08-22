using Elementum.Cli.Enums;

namespace Elementum.Cli.Input;

/// <summary>
/// Maps CLI metal enum to API symbol (XAU, XAG, XPT, XPD) and display name; parses key press (1–4, D1–D4, NumPad) to metal.
/// </summary>
public static class MetallHelper
{
    /// <summary>Returns the API symbol for the metal (e.g. XAU for Gold).</summary>
    public static string GetSymbol(Metall m) => m switch
    {
        Metall.Gold => "XAU",
        Metall.Silber => "XAG",
        Metall.Platin => "XPT",
        Metall.Palladium => "XPD",
        _ => ""
    };

    /// <summary>Returns the display name for the metal (e.g. Gold, Silver, Platinum, Palladium).</summary>
    public static string GetName(Metall m) => m switch
    {
        Metall.Gold => "Gold",
        Metall.Silber => "Silver",
        Metall.Platin => "Platinum",
        Metall.Palladium => "Palladium",
        _ => ""
    };

    /// <summary>Maps character '1'–'4' to Gold, Silber, Platin, Palladium; returns null otherwise.</summary>
    public static Metall? FromKey(char c)
    {
        return c switch
        {
            '1' => Metall.Gold,
            '2' => Metall.Silber,
            '3' => Metall.Platin,
            '4' => Metall.Palladium,
            _ => null
        };
    }

    /// <summary>Maps D1–D4 or NumPad1–4 to Gold, Silber, Platin, Palladium; returns null otherwise.</summary>
    public static Metall? FromConsoleKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => Metall.Gold,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => Metall.Silber,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => Metall.Platin,
            ConsoleKey.D4 or ConsoleKey.NumPad4 => Metall.Palladium,
            _ => null
        };
    }
}
