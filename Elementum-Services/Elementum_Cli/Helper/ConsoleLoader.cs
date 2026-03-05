namespace Elementum_Cli.Helper;

public class ConsoleLoader
{
    private static readonly char[] _sequence = { '|', '/', '-', '\\' };
    private static bool _active;
    private static int _counter;

    public static async Task RunAsync(Func<Task> action, string message = "Loading")
    {
        _active = true;

        var spinnerTask = Task.Run(() =>
        {
            while (_active)
            {
                Turn(message);
                Thread.Sleep(100);
            }
        });

        await action();

        _active = false;
        await spinnerTask;

        ClearLine();
    }

    public static async Task RunDotsAsync(Func<Task> action, string message = "Loading")
    {
        bool active = true;

        var task = Task.Run(() =>
        {
            int dots = 0;

            while (active)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{message}{new string('.', dots % 4)}   ");
                dots++;
                Thread.Sleep(300);
            }
        });

        await action();

        active = false;
        await task;

        Console.WriteLine();
    }

    private static void Turn(string message)
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write($"{message} {_sequence[_counter++ % _sequence.Length]}");
    }

    private static void ClearLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
    }
}