using Elementum_Cli.Enums;

namespace Elementum_Cli.Models;

/// <summary>
/// One entry in the main menu. Links display text, selectability, optional shortcut key, and the view to open on selection.
/// </summary>
public class MenuItem(string text, bool selectable, char? key = null, DetailView? view = null)
{
    public string Text { get; } = text;
    public bool Selectable { get; } = selectable;
    public char? Key { get; } = key;
    public DetailView? View { get; } = view;
}
