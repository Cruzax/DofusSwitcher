using System;
using System.Collections.Generic;
using System.Linq;

namespace DofusSwitcher.Core;

/// <summary>Identifiants des actions qui ne sont pas un perso (un perso est identifié par son pseudo).</summary>
public static class HotkeyOwners
{
    public const string Next = "@next";
    public const string Previous = "@previous";
    public const string Pause = "@pause";
}

/// <summary>Raccourci que Windows a refusé (déjà pris par une autre appli).</summary>
public sealed record HotkeyFailure(string Owner, string Label, string Hotkey);

public static class HotkeyConflicts
{
    /// <summary>Renvoie les actions qui partagent la même touche qu'une autre action.</summary>
    public static HashSet<string> FindDuplicates(IEnumerable<(string Owner, string? Hotkey)> entries)
    {
        var ownersByKey = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (owner, text) in entries)
        {
            if (!Hotkey.TryParse(text, out var hotkey)) continue;
            var key = hotkey.ToString();
            if (!ownersByKey.TryGetValue(key, out var owners)) ownersByKey[key] = owners = new();
            owners.Add(owner);
        }

        return ownersByKey.Values
            .Where(owners => owners.Count > 1)
            .SelectMany(owners => owners)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
