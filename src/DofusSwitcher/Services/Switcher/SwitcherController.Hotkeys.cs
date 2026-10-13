using System;
using System.Collections.Generic;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Raccourcis clavier globaux : enregistrement, pause, suspension pendant les réglages.</summary>
public sealed partial class SwitcherController
{
    private readonly HotkeyManager _hotkeys = new();
    private readonly List<HotkeyFailure> _hotkeyFailures = new();
    private bool _isPaused;
    private bool _hotkeysSuspended;

    /// <summary>Raccourcis refusés par Windows lors du dernier enregistrement.</summary>
    public IReadOnlyList<HotkeyFailure> HotkeyFailures => _hotkeyFailures;

    /// <summary>En pause, seuls les clics sur la barre et la touche de pause restent actifs.</summary>
    public bool IsPaused
    {
        get => _isPaused;
        set
        {
            if (!Set(ref _isPaused, value)) return;
            if (!_hotkeysSuspended) RegisterHotkeys();
        }
    }

    public void TogglePause() => IsPaused = !IsPaused;

    /// <summary>Désactive les raccourcis globaux (pour pouvoir les saisir dans les réglages).</summary>
    public void SuspendHotkeys()
    {
        _hotkeysSuspended = true;
        _hotkeys.UnregisterAll();
    }

    public void ResumeHotkeys()
    {
        _hotkeysSuspended = false;
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        _hotkeys.UnregisterAll();
        _hotkeyFailures.Clear();

        // La touche de pause reste active pendant la pause, sinon on ne pourrait pas reprendre au clavier.
        TryRegister(HotkeyOwners.Pause, "Pause / reprise", Config.PauseHotkey, TogglePause);
        if (IsPaused) return;

        foreach (var character in Config.Characters)
        {
            var name = character.Name;
            TryRegister(name, name, character.Hotkey, () => ActivateCharacter(name));
        }
        TryRegister(HotkeyOwners.Next, "Perso suivant", Config.NextHotkey, () => Step(1));
        TryRegister(HotkeyOwners.Previous, "Perso précédent", Config.PreviousHotkey, () => Step(-1));
    }

    private void TryRegister(string owner, string label, string? text, Action action)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!Hotkey.TryParse(text, out var hotkey) || !_hotkeys.Register(hotkey, action))
        {
            _hotkeyFailures.Add(new HotkeyFailure(owner, label, text));
        }
    }
}
