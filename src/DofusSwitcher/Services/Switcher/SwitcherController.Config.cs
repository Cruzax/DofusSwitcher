using System;
using System.IO;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Config : enregistrement, application des réglages, aperçu de la barre, classes et icônes des persos.</summary>
public sealed partial class SwitcherController
{
    /// <summary>Aperçu de la barre pendant les réglages, sans rien enregistrer.</summary>
    public void PreviewLayout(BarOrientation orientation, BarSize size) => Layout = BarLayout.For(orientation, size);

    /// <summary>Annule l'aperçu : revient à la mise en page enregistrée.</summary>
    public void RestoreLayout() => Layout = BarLayout.For(Config);

    public void SaveConfig()
    {
        try
        {
            _store.Save(Config);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Fichier bloqué un instant (antivirus, synchro…) : la config reste en mémoire, nouvel essai au prochain enregistrement.
            _errors.Write("Enregistrement de la config", ex);
        }
    }

    public void ApplyConfig(AppConfig config)
    {
        Config = config;
        SaveConfig();
        _scanner.SetProcessNames(config.ProcessNames);
        Layout = BarLayout.For(config);
        RebuildSlots();
        _hotkeysSuspended = false;
        RegisterHotkeys();
        Refresh();
        OnPropertyChanged(nameof(IsBarWanted));
        // Le réglage "perso actif en grand" a pu changer : la mosaïque en cours suit.
        if (IsMosaic) _animator.AnimateTo(MosaicTargets());
    }

    public void SaveBarPosition(double left, double top)
    {
        Config.BarLeft = left;
        Config.BarTop = top;
        SaveConfig();
    }

    // ----- Persos de la barre -----

    /// <summary>Retient la classe vue dans le titre, pour l'afficher même quand le client est fermé.</summary>
    private void RememberClasses()
    {
        bool changed = false;
        foreach (var window in LastScan)
        {
            var character = Config.FindCharacter(window.CharacterName);
            if (window.ClassName is null || character is null || character.ClassName == window.ClassName) continue;
            character.ClassName = window.ClassName;
            changed = true;
        }
        if (changed) SaveConfig();
    }

    private void UpdateClasses()
    {
        foreach (var slot in Slots)
        {
            var className = Config.FindCharacter(slot.Name)?.ClassName;
            slot.SetClass(className, Appearances.Get(className, slot.Name));
        }
    }

    private void RebuildSlots()
    {
        Slots.Clear();
        foreach (var character in Config.Characters)
        {
            Slots.Add(new CharacterSlot(character.Name, character.Hotkey, character.Initiative,
                Appearances.Get(character.ClassName, character.Name)));
        }
    }
}
