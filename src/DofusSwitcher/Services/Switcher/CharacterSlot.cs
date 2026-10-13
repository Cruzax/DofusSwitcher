using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Un perso tel qu'affiché dans la barre flottante.</summary>
public sealed class CharacterSlot : ObservableObject
{
    private bool _isOnline;
    private bool _isActive;
    private bool _isAlerting;
    private string? _className;
    private ClassAppearance _appearance;

    public CharacterSlot(string name, string? hotkeyText, int? initiative, ClassAppearance appearance)
    {
        Name = name;
        HotkeyText = hotkeyText ?? "";
        Initiative = initiative;
        _appearance = appearance;
    }

    public string Name { get; }
    public string HotkeyText { get; }
    public bool HasHotkey => HotkeyText.Length > 0;

    /// <summary>Initiative saisie dans les réglages (null = inconnue, rien n'est affiché).</summary>
    public int? Initiative { get; }
    public bool HasInitiative => Initiative is not null;
    public string InitiativeText => Initiative?.ToString() ?? "";

    public bool IsOnline
    {
        get => _isOnline;
        set => Set(ref _isOnline, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => Set(ref _isActive, value);
    }

    /// <summary>La fenêtre du perso a clignoté (MP, invitation…) et n'a pas encore été affichée.</summary>
    public bool IsAlerting
    {
        get => _isAlerting;
        set => Set(ref _isAlerting, value);
    }

    public ClassAppearance Appearance => _appearance;
    public string ToolTipText =>
        (_className is null ? Name : $"{Name} — {_className}") + (Initiative is null ? "" : $"\nInitiative : {Initiative}");

    public void SetClass(string? className, ClassAppearance appearance)
    {
        if (Set(ref _className, className)) OnPropertyChanged(nameof(ToolTipText));
        Set(ref _appearance, appearance, nameof(Appearance));
    }
}
