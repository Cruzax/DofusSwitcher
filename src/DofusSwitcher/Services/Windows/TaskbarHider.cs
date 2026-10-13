using System;
using System.Collections.Generic;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Cache la barre des tâches Windows (pendant la mosaïque, elle passerait par-dessus les clients du bas).
/// On ne fait que masquer la fenêtre : aucun réglage Windows n'est modifié, et un redémarrage de
/// l'Explorateur la fait réapparaître de toute façon.
/// </summary>
public static class TaskbarHider
{
    private static readonly List<IntPtr> Hidden = new();

    public static bool IsHidden => Hidden.Count > 0;

    public static void Hide()
    {
        if (IsHidden) return;
        foreach (var hwnd in FindTaskbars())
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) continue; // déjà cachée (masquage auto de Windows…) : on n'y touche pas
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
            Hidden.Add(hwnd);
        }
    }

    /// <summary>Réaffiche ce qui a été caché. Sans effet si rien ne l'est (appelable à la fermeture, en cas d'erreur…).</summary>
    public static void Show()
    {
        foreach (var hwnd in Hidden) NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNA);
        Hidden.Clear();
    }

    /// <summary>Barre principale + celles des autres écrans.</summary>
    private static IEnumerable<IntPtr> FindTaskbars()
    {
        var result = new List<IntPtr>();
        NativeMethods.EnumWindows((hwnd, _) =>
        {
            var name = NativeMethods.GetClassName(hwnd);
            if (name is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") result.Add(hwnd);
            return true;
        }, IntPtr.Zero);
        return result;
    }
}
