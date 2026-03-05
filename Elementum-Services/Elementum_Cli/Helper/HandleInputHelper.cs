using Elementum_Cli.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Helper
{
    public class HandleInputHelper
    {
        public static void HandleInput(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                currentState = AppState.Menu;
                currentDetailView = null;
                CliOutputHelper.RenderMenu();
            }
        }

        public static void HandleInputWithMetals(ConsoleKeyInfo key, Action renderMethod )
        {
            if (!currentSelectedMetal.HasValue)
            {
                var metall = MetallHelper.FromKey(key.KeyChar) ?? MetallHelper.FromConsoleKey(key.Key);
                if (metall.HasValue)
                {
                    currentSelectedMetal = metall;
                    _ = renderMethod;
                }
                else if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
                {
                    currentState = AppState.Menu;
                    currentDetailView = null;
                    CliOutputHelper.RenderMenu();
                }
                return;
            }
            if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.LeftArrow)
            {
                currentState = AppState.Menu;
                currentDetailView = null;
                currentSelectedMetal = null;
                CliOutputHelper.RenderMenu();
            }
        }
    }
}
