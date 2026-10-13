using DofusSwitcher.Core;

namespace DofusSwitcher.UI;

/// <summary>Une action à laquelle on assigne une touche dans les réglages.</summary>
public class HotkeyField : ObservableObject
{
    private string? _hotkey;
    private bool _hasError;

    public HotkeyField(string owner, string label, string? hotkey)
    {
        Owner = owner;
        Label = label;
        _hotkey = hotkey;
    }

    /// <summary>Pseudo du perso, ou <see cref="HotkeyOwners"/> pour la navigation.</summary>
    public string Owner { get; }
    public string Label { get; }

    public string? Hotkey
    {
        get => _hotkey;
        set => Set(ref _hotkey, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => Set(ref _hasError, value);
    }
}
