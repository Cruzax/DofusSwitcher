using System;
using System.Windows;
using System.Windows.Threading;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>Clics de la barre : icônes des persos, pause, mosaïque, tout fermer et menu du clic droit.</summary>
public partial class FloatingBar
{
    private void Slot_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressNextClick)
        {
            _suppressNextClick = false;
            return;
        }
        if (sender is FrameworkElement { Tag: CharacterSlot slot }) _controller.ActivateCharacter(slot.Name);
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _controller.TogglePause();

    private async void Mosaic_Click(object sender, RoutedEventArgs e) => await _controller.ToggleMosaicAsync();

    /// <summary>Après le bouton ou la touche de mosaïque : explique pourquoi certains clients n'ont pas bougé.</summary>
    private void OnFullscreenClientsSkipped(int count)
    {
        MessageBox.Show(
            (count == 1 ? "Un client est réglé" : $"{count} clients sont réglés")
            + " sur « Plein écran » dans Dofus et ne peut pas être rangé.\n\n"
            + "Dans Dofus : Options → Affichage → Mode d'affichage → « Fenêtré ». Le bouton s'occupe ensuite "
            + "du plein écran (sans bordure) et de la mosaïque.",
            "Dofus Switcher", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ----- Fermer tous les clients -----
    // Un clic de travers ne doit pas couper toute la team : le 1er clic arme le bouton, le 2e (sous 3 s) ferme.

    private DispatcherTimer? _closeAllDisarm;

    private async void CloseAll_Click(object sender, RoutedEventArgs e)
    {
        if (CloseAllButton.Tag as string == "Armed")
        {
            DisarmCloseAll();
            await _controller.CloseAllGameWindowsAsync();
            return;
        }

        int count = _controller.CountGameWindows();
        if (count == 0)
        {
            CloseAllButton.ToolTip = "Aucun client Dofus ouvert";
        }
        else
        {
            CloseAllButton.Tag = "Armed";
            CloseAllButton.ToolTip = count == 1 ? "Clique encore pour fermer le client" : $"Clique encore pour fermer les {count} clients";
        }
        // Le minuteur désarme le bouton et remet l'info-bulle d'origine.
        _closeAllDisarm ??= CreateDisarmTimer();
        _closeAllDisarm.Stop();
        _closeAllDisarm.Start();
    }

    private DispatcherTimer CreateDisarmTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        timer.Tick += (_, _) => DisarmCloseAll();
        return timer;
    }

    private void DisarmCloseAll()
    {
        _closeAllDisarm?.Stop();
        CloseAllButton.ClearValue(TagProperty);
        CloseAllButton.ClearValue(ToolTipProperty);
    }

    private void BarMenu_Opened(object sender, RoutedEventArgs e) =>
        PauseMenuItem.Header = _controller.IsPaused ? "Reprendre" : "Mettre en pause";

    // Pour la réafficher : icône près de l'horloge → Afficher / masquer la barre.
    private void HideBar_Click(object sender, RoutedEventArgs e) => ToggleHiddenByUser();

    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsRequested?.Invoke();

    private void Quit_Click(object sender, RoutedEventArgs e) => QuitRequested?.Invoke();
}
