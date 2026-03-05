using Elementum_Cli.Enums;

namespace Elementum_Cli.Models
{
    public class MenuItem
    {
        public string Text { get; }
        public bool Selectable { get; }
        public char? Key { get; }
        public DetailView? View { get; }

        public MenuItem(string text, bool selectable, char? key = null, DetailView? view = null)
        {
            Text = text;
            Selectable = selectable;
            Key = key;
            View = view;
        }
    }
}
