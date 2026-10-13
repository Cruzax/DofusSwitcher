using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class HotkeyConflictsTests
{
    [Fact]
    public void Flags_every_owner_sharing_a_key()
    {
        var duplicates = HotkeyConflicts.FindDuplicates(new (string, string?)[]
        {
            ("Perso-Un", "F1"),
            ("Perso-Deux", "f1"),
            ("Perso-Trois", "F3"),
            (HotkeyOwners.Next, "F3"),
            (HotkeyOwners.Previous, "F5"),
        });

        Assert.Equal(4, duplicates.Count);
        Assert.Contains("Perso-Un", duplicates);
        Assert.Contains("Perso-Deux", duplicates);
        Assert.Contains("Perso-Trois", duplicates);
        Assert.Contains(HotkeyOwners.Next, duplicates);
    }

    [Fact]
    public void Ignores_empty_and_invalid_hotkeys()
    {
        var duplicates = HotkeyConflicts.FindDuplicates(new (string, string?)[]
        {
            ("A", null),
            ("B", ""),
            ("C", "Ctrl"),
            ("D", "Ctrl"),
            ("E", "Ctrl+F1"),
            ("F", "F1"),
        });

        Assert.Empty(duplicates);
    }
}
