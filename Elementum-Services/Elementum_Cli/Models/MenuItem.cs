using Elementum_Cli.Enums;

namespace Elementum_Cli.Models;

public class MenuItem(string text, bool selectable, char? key = null, DetailView? view = null)
{
    public string Text { get; } = text;
    public bool Selectable { get; } = selectable;
    public char? Key { get; } = key;
    public DetailView? View { get; } = view;
}
