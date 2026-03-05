namespace Elementum.Shared.Helper;

public class ConsoleOutput
{
    public static void WriteLine(string message, ConsoleColor color = ConsoleColor.White)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previousColor;
    }

    public static void Write(string message, ConsoleColor color = ConsoleColor.White)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = previousColor;
    }

    public static void WriteError(string message)
    {
        WriteLine(message, ConsoleColor.Red);
    }

    public static void WriteSuccess(string message)
    {
        WriteLine(message, ConsoleColor.Green);
    }

    public static void WriteWarning(string message)
    {
        WriteLine(message, ConsoleColor.Yellow);
    }

    public static void WriteInfo(string message)
    {
        WriteLine(message, ConsoleColor.Cyan);
    }

    public static void WriteDebug(string message)
    {
        WriteLine(message, ConsoleColor.Magenta);
    }
}
