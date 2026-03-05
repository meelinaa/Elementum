using Elementum_Cli.Enums;

namespace Elementum_Cli.Helper;

public static class MetallHelper
{
    public static string GetSymbol(Metall m) => m switch
    {
        Metall.Gold => "XAU",
        Metall.Silber => "XAG",
        Metall.Platin => "XPT",
        _ => ""
    };

    public static string GetName(Metall m) => m switch
    {
        Metall.Gold => "Gold",
        Metall.Silber => "Silver",
        Metall.Platin => "Platinum",
        _ => ""
    };

    public static Metall? FromKey(char c)
    {
        return c switch
        {
            '1' => Metall.Gold,
            '2' => Metall.Silber,
            '3' => Metall.Platin,
            _ => null
        };
    }

    public static Metall? FromConsoleKey(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.D1 or ConsoleKey.NumPad1 => Metall.Gold,
            ConsoleKey.D2 or ConsoleKey.NumPad2 => Metall.Silber,
            ConsoleKey.D3 or ConsoleKey.NumPad3 => Metall.Platin,
            _ => null
        };
    }
}
