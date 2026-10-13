using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DofusSwitcher.Core;

namespace DofusSwitcher.UI;

/// <summary>Équipe : ajout (persos connectés ou pseudo tapé), ordre, retrait et initiative.</summary>
public partial class SettingsWindow
{
    private static readonly string[] DefaultCharacterHotkeys = { "F1", "F2", "F3", "F4" };

    private EditableCharacter CreateCharacter(string name, string? className, string? hotkey, int? initiative = null) =>
        new(name, className, hotkey, initiative, _controller.Appearances.Get(className, name));

    private void UpdateCharacterCount()
    {
        CountText.Text = $"{Characters.Count} sur {AppConfig.MaxCharacters}";
        bool canAdd = Characters.Count < AppConfig.MaxCharacters;
        AddSlot.Visibility = canAdd && AddPanel.Visibility != Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
        if (!canAdd) AddPanel.Visibility = Visibility.Collapsed;
    }

    // ----- Ajout de persos -----

    private void AddSlot_Click(object sender, RoutedEventArgs e)
    {
        AddPanel.Visibility = Visibility.Visible;
        AddSlot.Visibility = Visibility.Collapsed;
        RefreshDetected();
    }

    private void RefreshDetected()
    {
        _controller.Refresh();
        Detected.Clear();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var window in _controller.LastScan)
        {
            if (Contains(window.CharacterName) || !seen.Add(window.CharacterName)) continue;
            Detected.Add(new DetectedCharacter(window.CharacterName, window.ClassName,
                _controller.Appearances.Get(window.ClassName, window.CharacterName)));
        }
        NoDetectedHint.Visibility = Detected.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool Contains(string name) =>
        Characters.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));

    private void AddCharacter(string name, string? className)
    {
        name = name.Trim();
        if (name.Length == 0 || Contains(name) || Characters.Count >= AppConfig.MaxCharacters) return;

        var used = AllHotkeyFields.Select(f => f.Hotkey).ToList();
        var hotkey = DefaultCharacterHotkeys.FirstOrDefault(k => !used.Contains(k, StringComparer.OrdinalIgnoreCase));
        Characters.Add(CreateCharacter(name, className, hotkey));

        AddPanel.Visibility = Visibility.Collapsed;
        UpdateCharacterCount();
        Validate();
    }

    private void AddDetected_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DetectedCharacter detected }) AddCharacter(detected.Name, detected.ClassName);
    }

    private void AddManual_Click(object sender, RoutedEventArgs e) => AddManual();

    private void ManualName_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        AddManual();
        e.Handled = true;
    }

    private void AddManual()
    {
        var name = ManualName.Text;
        ManualName.Text = "";
        // Un perso connecté garde sa classe même s'il est ajouté à la main.
        var className = _controller.LastScan
            .FirstOrDefault(w => string.Equals(w.CharacterName, name.Trim(), StringComparison.OrdinalIgnoreCase))?.ClassName;
        AddCharacter(name, className);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshDetected();

    // ----- Liste des persos -----

    private void MoveUp_Click(object sender, RoutedEventArgs e) => Move(sender, -1);

    private void MoveDown_Click(object sender, RoutedEventArgs e) => Move(sender, 1);

    private void Move(object sender, int offset)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        int index = Characters.IndexOf(character);
        int target = index + offset;
        if (index >= 0 && target >= 0 && target < Characters.Count) Characters.Move(index, target);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        Characters.Remove(character);
        Validate();
        if (AddPanel.Visibility == Visibility.Visible) RefreshDetected();
    }

    // ----- Initiative -----

    /// <summary>Range l'équipe dans l'ordre des tours : la plus haute initiative d'abord, les inconnues à la fin.</summary>
    private void SortByInitiative_Click(object sender, RoutedEventArgs e)
    {
        var sorted = InitiativeOrder.Sort(Characters, c => c.Initiative);
        for (int target = 0; target < sorted.Count; target++)
        {
            int current = Characters.IndexOf(sorted[target]);
            if (current != target) Characters.Move(current, target);
        }
    }

    private void Initiative_LostFocus(object sender, RoutedEventArgs e) => Validate();
}
