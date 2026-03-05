using Elementum_Cli.Enums;
using Elementum_Cli.Helper;
using Elementum_Cli.Models;
using System.Text;

namespace Elementum_Cli
{
    public class Program
    {
        public static AppState currentState = AppState.Menu;
        public static DetailView? currentDetailView;
        public static Metall? currentSelectedMetal;
        public static bool running = true;
        public static int selectedIndex = 0;

        public static Dictionary<int, int> rowMap = new();

        public static List<MenuItem> menuItems = new()
        {
            new MenuItem("DASHBOARD         - All Metals (Price & Change %)", true, 'D', DetailView.Dashboard),
        
            new MenuItem("", false),
        
            new MenuItem("TRADING & DAILY ANALYSIS - Bid/Ask, Compare, Volatility", true, 'T', DetailView.TradingView),
            new MenuItem("KARAT & ALLOY - Price per gram + discount vs. 24k", true, 'K', DetailView.KaratCalculator),
        
            new MenuItem("", false),

            new MenuItem("List of All Metals- (Gold, Silver, Platinum)", true, 'L', DetailView.ListMetals),
            new MenuItem("HISTORY           - Select Entry Date", true, 'H', DetailView.History),
        
            new MenuItem("", false),
        
            new MenuItem("INFO              - Core Data & Creation Date", true, 'I', DetailView.Info),
            new MenuItem("[ESC] Exit Program", true, 'X', null)
        };

        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.CursorVisible = false;
            CliNavigation.EnsureValidStartIndex();
            CliOutputHelper.RenderMenu();

            while (running)
            {
                CliNavigation.HandleInput();
            }
        }
    }
}
