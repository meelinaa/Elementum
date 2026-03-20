using Elementum_Cli.Enums;

namespace Elementum_Cli.Models;

/// <summary>
/// One entry in the main menu. Links display text, selectability, optional shortcut key, and the view to open on selection.
/// </summary>
public record MenuItem(string Text, bool Selectable, char? Key = null, DetailView? View = null);
