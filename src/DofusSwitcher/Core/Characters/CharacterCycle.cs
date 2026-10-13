using System;
using System.Collections.Generic;

namespace DofusSwitcher.Core;

public static class CharacterCycle
{
    /// <summary>
    /// Renvoie le perso suivant (direction = 1) ou précédent (direction = -1) dans l'ordre donné,
    /// en sautant ceux qui sont hors ligne. Null si aucun perso n'est en ligne.
    /// </summary>
    public static string? Step(IReadOnlyList<string> order, Func<string, bool> isOnline, string? current, int direction)
    {
        if (order.Count == 0) return null;
        direction = direction >= 0 ? 1 : -1;

        int start = current is null ? -1 : IndexOf(order, current);
        // Sans perso courant connu, le premier pas tombe sur le premier (ou le dernier) de la liste.
        if (start < 0) start = direction > 0 ? -1 : order.Count;

        for (int i = 1; i <= order.Count; i++)
        {
            int index = ((start + direction * i) % order.Count + order.Count) % order.Count;
            if (isOnline(order[index])) return order[index];
        }
        return null;
    }

    private static int IndexOf(IReadOnlyList<string> order, string name)
    {
        for (int i = 0; i < order.Count; i++)
        {
            if (string.Equals(order[i], name, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }
}
