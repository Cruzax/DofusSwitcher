using System;
using System.Collections.Generic;
using System.Linq;

namespace DofusSwitcher.Core;

/// <summary>Ordre des tours en combat : la plus haute initiative joue en premier.</summary>
public static class InitiativeOrder
{
    /// <summary>
    /// Trie de la plus haute à la plus basse initiative. Les persos sans initiative passent à la fin ; à égalité,
    /// l'ordre d'origine est gardé.
    /// </summary>
    public static List<T> Sort<T>(IEnumerable<T> items, Func<T, int?> initiative) =>
        items.Select((item, index) => (item, index))
            .OrderBy(x => initiative(x.item) is null ? 1 : 0)
            .ThenByDescending(x => initiative(x.item) ?? 0)
            .ThenBy(x => x.index)
            .Select(x => x.item)
            .ToList();

    /// <summary>"1 250" ou "1250" → 1250. Vide → null. Refuse le reste.</summary>
    public static bool TryParse(string? text, out int? initiative)
    {
        initiative = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        var digits = new string(text.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (!int.TryParse(digits, out int value) || value < 0) return false;
        initiative = value;
        return true;
    }
}
