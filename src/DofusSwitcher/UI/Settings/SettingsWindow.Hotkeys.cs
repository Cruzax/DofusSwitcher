using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DofusSwitcher.Core;

namespace DofusSwitcher.UI;

/// <summary>Raccourcis : saisie d'une touche au clavier, retrait, et vérification avant d'enregistrer.</summary>
public partial class SettingsWindow
{
    /// <summary>Perso sans touche : on l'affiche en cliquant sur la barre ou avec Suivant / Précédent.</summary>
    private void ClearHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: EditableCharacter character }) return;
        character.Hotkey = null;
        Validate();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.Tab || Hotkey.IsModifierKey(key)) return;

        box.Text = key is Key.Delete or Key.Back or Key.Escape ? "" : new Hotkey(Keyboard.Modifiers, key).ToString();
        box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        Keyboard.ClearFocus();
        Validate();
    }

    /// <summary>Signale en rouge les touches utilisées par plusieurs actions. Renvoie false s'il y en a.</summary>
    private bool Validate()
    {
        var duplicates = HotkeyConflicts.FindDuplicates(AllHotkeyFields.Select(f => (f.Owner, f.Hotkey)));
        foreach (var field in AllHotkeyFields) field.HasError = duplicates.Contains(field.Owner);

        if (duplicates.Count > 0)
        {
            ShowStatus("Une même touche est utilisée pour plusieurs actions.");
            return false;
        }
        if (Characters.Any(c => c.HasInitiativeError))
        {
            ShowStatus("L'initiative doit être un nombre (ex. 1250), ou rester vide.");
            return false;
        }
        HideStatus();
        return true;
    }
}
