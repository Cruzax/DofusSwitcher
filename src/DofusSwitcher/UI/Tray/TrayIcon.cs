using System;
using System.Windows;
using DofusSwitcher.Services;
using Forms = System.Windows.Forms;

namespace DofusSwitcher.UI;

/// <summary>Icône près de l'horloge : pause, réglages, afficher / masquer la barre, quitter (double clic : réglages).</summary>
public sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon _icon;

    public TrayIcon(SwitcherController controller, Action openSettings, Action toggleBar, Action quit)
    {
        var menu = new Forms.ContextMenuStrip();
        var pauseItem = menu.Items.Add("", null, (_, _) => controller.TogglePause());
        menu.Opening += (_, _) => pauseItem.Text = controller.IsPaused ? "Reprendre" : "Mettre en pause";
        menu.Items.Add("Réglages", null, (_, _) => openSettings());
        menu.Items.Add("Afficher / masquer la barre", null, (_, _) => toggleBar());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quitter", null, (_, _) => quit());

        _icon = new Forms.NotifyIcon
        {
            Icon = LoadIcon(),
            Text = "Dofus Switcher",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => openSettings();
    }

    private static System.Drawing.Icon LoadIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"));
        if (resource is null) return System.Drawing.SystemIcons.Application;
        using var stream = resource.Stream;
        // Taille adaptée à la zone de notification (16 px à 100 %, plus en haute résolution).
        return new System.Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
    }

    public void Dispose()
    {
        // Masquée d'abord : sinon l'icône reste affichée jusqu'au survol de la zone de notification.
        _icon.Visible = false;
        _icon.Dispose();
    }
}
