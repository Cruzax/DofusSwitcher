using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DofusSwitcher.Core;
using DofusSwitcher.Services;
using DofusSwitcher.Win32;

namespace DofusSwitcher.UI;

/// <summary>Étiquette d'un client en mosaïque. Laisse passer les clics vers le jeu et ne prend jamais le focus.</summary>
public partial class TileLabel : Window
{
    public TileLabel()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, style | NativeMethods.WS_EX_TRANSPARENT
                | NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
        };
    }

    /// <summary>Centre l'étiquette en haut de la case du client (coordonnées écran en pixels).</summary>
    public void Place(CharacterSlot slot, TileRect tile)
    {
        DataContext = slot;
        if (!IsVisible) Show();
        UpdateLayout();

        // La case est en pixels physiques, la taille de l'étiquette en unités WPF : on convertit.
        var dpi = VisualTreeHelper.GetDpi(this);
        int width = (int)Math.Ceiling(ActualWidth * dpi.DpiScaleX);
        int x = tile.X + (tile.Width - width) / 2;
        // Tout en haut de la fenêtre : la marge de l'étiquette (6) suffit à la décoller du bord.
        int y = tile.Y;
        NativeMethods.SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero, x, y, 0, 0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
    }
}
