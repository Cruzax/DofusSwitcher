using DofusSwitcher.Core;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>Un perso de l'équipe en cours d'édition dans les réglages (touche, initiative).</summary>
public sealed class EditableCharacter : HotkeyField
{
    private string _initiativeText;
    private bool _hasInitiativeError;

    public EditableCharacter(string name, string? className, string? hotkey, int? initiative, ClassAppearance appearance)
        : base(name, name, hotkey)
    {
        ClassName = className;
        Appearance = appearance;
        _initiativeText = initiative?.ToString() ?? "";
    }

    public string Name => Owner;
    public string? ClassName { get; }
    public ClassAppearance Appearance { get; }
    public string Subtitle => ClassName ?? "Classe inconnue";

    /// <summary>Initiative telle que tapée (vide = inconnue).</summary>
    public string InitiativeText
    {
        get => _initiativeText;
        set
        {
            if (!Set(ref _initiativeText, value)) return;
            HasInitiativeError = !InitiativeOrder.TryParse(value, out _);
        }
    }

    public bool HasInitiativeError
    {
        get => _hasInitiativeError;
        private set => Set(ref _hasInitiativeError, value);
    }

    /// <summary>Valeur retenue (null si vide ou invalide).</summary>
    public int? Initiative => InitiativeOrder.TryParse(InitiativeText, out var value) ? value : null;
}
