using Elementum_Cli.Enums;
using Elementum_Cli.Views;
using static Elementum_Cli.Program;

namespace Elementum_Cli.Helper
{
    public class CliOutputHelper
    {
        public const int ViewWidth = 72;

        public static void RenderViewHeader(string title)
        {
            Console.WriteLine();
            Console.WriteLine("╔" + new string('═', ViewWidth - 2) + "╗");
            Console.WriteLine("║ " + title.PadRight(ViewWidth - 4) + " ║");
            Console.WriteLine("╠" + new string('═', ViewWidth - 2) + "╣");
        }

        public static void RenderSectionTitle(string title)
        {
            Console.WriteLine();
            Console.WriteLine("  ┌" + new string('─', ViewWidth - 6) + "┐");
            Console.WriteLine("  │ " + title.PadRight(ViewWidth - 8) + " │");
            Console.WriteLine("  ├" + new string('─', ViewWidth - 6) + "┤");
        }

        public static void RenderViewFooter()
        {
            Console.WriteLine();
            Console.WriteLine("╟" + new string('─', ViewWidth - 2) + "╢");
            Console.WriteLine("║ [ESC/←] Back to menu".PadRight(ViewWidth - 2) + " ║");
            Console.WriteLine("╚" + new string('═', ViewWidth - 2) + "╝");
            Console.WriteLine();
        }

        public static void RenderMetalSelectionPrompt()
        {
            RenderViewHeader("SELECT METAL");
            Console.WriteLine("  Which metal should data be displayed for?");
            Console.WriteLine();
            Console.WriteLine("    [1] Gold        [2] Silver      [3] Platinum ");
            Console.WriteLine();
            Console.WriteLine("  Press 1–4 to select. [ESC/←] Back to menu.");
            RenderViewFooter();
        }

        public static void DrawMenuItem(int index, bool selected)
        {
            if (!rowMap.ContainsKey(index))
                return;

            Console.SetCursorPosition(0, rowMap[index]);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, rowMap[index]);

            if (!menuItems[index].Selectable)
            {
                Console.Write("   " + menuItems[index].Text);
                return;
            }

            if (selected)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(" ▶ " + menuItems[index].Text);
                Console.ResetColor();
            }
            else
            {
                Console.Write("   " + menuItems[index].Text);
            }
        }

        public static void OpenSelectedPage()
        {
            var item = menuItems[selectedIndex];
            if (item.View == null || item.Text.Contains("exit", StringComparison.OrdinalIgnoreCase))
            {
                running = false;
                return;
            }

            currentState = AppState.Detail;
            currentDetailView = item.View;
            if (ViewRegistry.RequiresMetalSelection(item.View.Value))
                currentSelectedMetal = null;
            var view = ViewRegistry.Get(item.View.Value);
            view.Render();
        }

        public static void RenderMenu()
        {
            Console.Clear();
            rowMap.Clear();

            //RenderTageswerteHeader();
            Console.WriteLine("=================================================================================");
            Console.WriteLine($"                         ELEMENTUM - METALS DASHBOARD");
            Console.WriteLine("=================================================================================");
            Console.WriteLine();
            Console.WriteLine("                 [↑/↓] Navigate | [Enter/→] Select | [ESC] Exit");
            Console.WriteLine();

            for (int i = 0; i < menuItems.Count; i++)
            {
                if (!menuItems[i].Selectable && string.IsNullOrWhiteSpace(menuItems[i].Text))
                {
                    Console.WriteLine();
                    continue;
                }

                rowMap[i] = Console.CursorTop;
                DrawMenuItem(i, i == selectedIndex);
                Console.WriteLine();
            }

            Console.WriteLine();
            Console.WriteLine("---------------------------------------------------------------------------------");
            Console.WriteLine("                        Arrow keys + Enter to navigate                             ");
            Console.WriteLine("=================================================================================");
        }
    }
}
