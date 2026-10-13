using System;
using System.Collections.Generic;
using System.Linq;

namespace DofusSwitcher.Core;

public enum BarOrientation
{
    Horizontal,
    Vertical,
}

public enum BarSize
{
    Small,
    Normal,
    Large,
}

public sealed class CharacterConfig
{
    public string Name { get; set; } = "";
    public string? Hotkey { get; set; }

    /// <summary>Dernière classe vue dans le titre de la fenêtre (gardée pour l'affichage hors ligne).</summary>
    public string? ClassName { get; set; }

    /// <summary>Initiative saisie par l'utilisateur (fiche du perso en jeu), pour ranger l'équipe dans l'ordre des tours.</summary>
    public int? Initiative { get; set; }
}

public sealed class AppConfig
{
    public const int MaxCharacters = 8;

    public List<CharacterConfig> Characters { get; set; } = new();
    public string? NextHotkey { get; set; } = "F6";
    public string? PreviousHotkey { get; set; } = "F5";

    /// <summary>Touche qui met en pause / réactive les raccourcis (aucune par défaut).</summary>
    public string? PauseHotkey { get; set; }

    /// <summary>Touche qui bascule entre mosaïque et plein écran, comme le bouton de la barre (aucune par défaut).</summary>
    public string? MosaicHotkey { get; set; }

    /// <summary>Noms de processus (sans .exe) considérés comme des clients Dofus.</summary>
    public List<string> ProcessNames { get; set; } = new() { "Dofus" };

    public double? BarLeft { get; set; }
    public double? BarTop { get; set; }
    public BarOrientation BarOrientation { get; set; } = BarOrientation.Horizontal;
    public BarSize BarSize { get; set; } = BarSize.Normal;

    /// <summary>N'affiche la barre que quand un client Dofus (ou l'appli elle-même) est au premier plan.</summary>
    public bool AutoHideBar { get; set; } = true;

    /// <summary>En mosaïque, le perso au premier plan prend la grande place (transition animée).</summary>
    public bool MosaicFocusActive { get; set; } = true;

    public CharacterConfig? FindCharacter(string name) =>
        Characters.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Déplace un perso à une nouvelle position (l'ordre sert aussi pour Suivant / Précédent).</summary>
    public bool MoveCharacter(string name, int newIndex)
    {
        var character = FindCharacter(name);
        if (character is null) return false;
        int oldIndex = Characters.IndexOf(character);
        newIndex = Math.Clamp(newIndex, 0, Characters.Count - 1);
        if (oldIndex == newIndex) return false;
        Characters.RemoveAt(oldIndex);
        Characters.Insert(newIndex, character);
        return true;
    }
}
