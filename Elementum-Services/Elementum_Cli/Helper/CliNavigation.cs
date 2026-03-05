using Elementum_Cli.Enums;
using Elementum_Cli.Views;
using static Elementum_Cli.Helper.CliOutputHelper;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Helper
{
    public class CliNavigation
    {
        public static void HandleInput()
        {
            var key = Console.ReadKey(true);

            if (currentState == AppState.Menu)
            {
                int previousIndex = selectedIndex;

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        MoveUp();
                        break;

                    case ConsoleKey.DownArrow:
                        MoveDown();
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.RightArrow:
                        OpenSelectedPage();
                        return;

                    case ConsoleKey.Escape:
                        running = false;
                        return;
                }

                if (previousIndex != selectedIndex)
                {
                    DrawMenuItem(previousIndex, false);
                    DrawMenuItem(selectedIndex, true);
                }
            }
            else if (currentState == AppState.Detail && currentDetailView.HasValue)
            {
                var view = ViewRegistry.Get(currentDetailView.Value);
                view.HandleInput(key);
                //HandleInputHelper.HandleInput(key);
            }
        }

        public static void MoveUp()
        {
            do
            {
                selectedIndex--;
                if (selectedIndex < 0)
                    selectedIndex = menuItems.Count - 1;
            }
            while (!menuItems[selectedIndex].Selectable);
        }

        public static void MoveDown()
        {
            do
            {
                selectedIndex++;
                if (selectedIndex >= menuItems.Count)
                    selectedIndex = 0;
            }
            while (!menuItems[selectedIndex].Selectable);
        }

        public static void EnsureValidStartIndex()
        {
            while (!menuItems[selectedIndex].Selectable)
            {
                selectedIndex++;
            }
        }
    }
}
