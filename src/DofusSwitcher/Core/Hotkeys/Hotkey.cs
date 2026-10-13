using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace DofusSwitcher.Core;

/// <summary>Combinaison de touches, sérialisée sous la forme "Ctrl+Shift+F1".</summary>
public readonly record struct Hotkey(ModifierKeys Modifiers, Key Key)
{
    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var modifiers = ModifierKeys.None;
        Key? key = null;
        foreach (var raw in text.Split('+'))
        {
            var part = raw.Trim();
            switch (part.ToLowerInvariant())
            {
                case "ctrl":
                case "control":
                    modifiers |= ModifierKeys.Control;
                    break;
                case "alt":
                    modifiers |= ModifierKeys.Alt;
                    break;
                case "shift":
                    modifiers |= ModifierKeys.Shift;
                    break;
                case "win":
                    modifiers |= ModifierKeys.Windows;
                    break;
                default:
                    if (key is not null || !TryParseKey(part, out var parsed)) return false;
                    key = parsed;
                    break;
            }
        }

        if (key is null) return false;
        hotkey = new Hotkey(modifiers, key.Value);
        return true;
    }

    public static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or
        Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin or Key.System;

    private static bool TryParseKey(string text, out Key key)
    {
        key = Key.None;
        // Les chiffres du haut du clavier s'écrivent "1" plutôt que "D1".
        if (text.Length == 1 && char.IsDigit(text[0]))
        {
            key = Key.D0 + (text[0] - '0');
            return true;
        }
        // Enum.TryParse accepte aussi les nombres ("5"), qu'on refuse.
        if (text.Length == 0 || char.IsDigit(text[0])) return false;
        return Enum.TryParse(text, ignoreCase: true, out key) && key != Key.None && !IsModifierKey(key);
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(Key is >= Key.D0 and <= Key.D9 ? ((int)(Key - Key.D0)).ToString() : Key.ToString());
        return string.Join("+", parts);
    }
}
