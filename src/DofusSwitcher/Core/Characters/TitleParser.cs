using System;

namespace DofusSwitcher.Core;

public sealed record ParsedTitle(string CharacterName, string? ClassName);

/// <summary>Lit un titre de fenêtre Dofus 3 ("Pseudo - Classe - Version - Release").</summary>
public static class TitleParser
{
    private const string Separator = " - ";

    public static ParsedTitle? Parse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var parts = title.Split(Separator);
        if (parts.Length < 2) return null;

        var name = parts[0].Trim();
        // Écran de connexion / sélection : le titre commence par "Dofus".
        if (name.Length == 0 || name.StartsWith("Dofus", StringComparison.OrdinalIgnoreCase)) return null;

        var className = parts[1].Trim();
        // Si la 2e partie est déjà la version, il n'y a pas de classe dans le titre.
        if (className.Length == 0 || char.IsDigit(className[0])) className = null;

        return new ParsedTitle(name, className);
    }
}
