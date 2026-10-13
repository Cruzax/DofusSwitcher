using System;
using System.Runtime.InteropServices;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Place les fenêtres des clients (mosaïque ou plein écran sans bordure). Ne touche qu'à la fenêtre,
/// comme le ferait Windows : rien n'est envoyé au jeu.
/// </summary>
public static class WindowArranger
{
    /// <summary>
    /// Vrai si le client est réglé sur "Plein écran" dans Dofus : il impose alors sa taille et ne se laisse
    /// pas ranger. Il faut le mode "Fenêtré" (Options → Affichage).
    /// </summary>
    public static bool IsGameFullscreenMode(IntPtr hwnd) =>
        (NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE) & NativeMethods.WS_POPUP) != 0;

    /// <summary>Écran entier (barre des tâches comprise) sur lequel se trouve la fenêtre.</summary>
    public static TileRect ScreenOf(IntPtr hwnd)
    {
        var info = new NativeMethods.MONITORINFO { cbSize = Marshal.SizeOf<NativeMethods.MONITORINFO>() };
        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        NativeMethods.GetMonitorInfo(monitor, ref info);
        var r = info.rcMonitor;
        return new TileRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);
    }

    /// <summary>
    /// Retire la barre de titre et les bords, puis place la fenêtre sur la case. Dofus réagit au retrait des bords
    /// un peu plus tard en reprenant sa résolution : vérifier ensuite avec <see cref="IsPlaced"/> et replacer.
    /// </summary>
    public static void PlaceBorderless(IntPtr hwnd, TileRect tile)
    {
        // Une fenêtre agrandie ignore les changements de taille : on la "restaure" d'abord, sans l'activer.
        if (NativeMethods.IsZoomed(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);

        int style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE,
            style & ~NativeMethods.WS_FRAME_PARTS & ~NativeMethods.WS_MAXIMIZE);
        Move(hwnd, tile, NativeMethods.SWP_FRAMECHANGED);
    }

    /// <summary>Remet la fenêtre sur sa case (sans toucher à son style).</summary>
    public static void Move(IntPtr hwnd, TileRect tile, uint extraFlags = 0) =>
        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, tile.X, tile.Y, tile.Width, tile.Height,
            NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | extraFlags);

    /// <summary>
    /// Signale la fin d'un redimensionnement, comme quand on lâche le bord d'une fenêtre à la souris :
    /// certains jeux (Unity) n'adoptent leur nouvelle taille qu'à ce moment-là.
    /// </summary>
    public static void NotifyResized(IntPtr hwnd)
    {
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_ENTERSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
        NativeMethods.PostMessage(hwnd, NativeMethods.WM_EXITSIZEMOVE, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>Vrai si la fenêtre occupe exactement la case (ou n'existe plus : rien à corriger).</summary>
    public static bool IsPlaced(IntPtr hwnd, TileRect tile)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var r)) return true;
        return r.Left == tile.X && r.Top == tile.Y && r.Right - r.Left == tile.Width && r.Bottom - r.Top == tile.Height;
    }
}
