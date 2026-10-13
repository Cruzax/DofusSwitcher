using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

public sealed record GameWindow(IntPtr Handle, string Title, string CharacterName, string? ClassName);

/// <summary>Liste les fenêtres visibles des clients Dofus qui ont un perso connecté.</summary>
public sealed class WindowScanner
{
    private HashSet<string> _processNames;

    public WindowScanner(IEnumerable<string> processNames) => _processNames = ToSet(processNames);

    public void SetProcessNames(IEnumerable<string> processNames) => _processNames = ToSet(processNames);

    public IReadOnlyList<GameWindow> Scan()
    {
        var result = new List<GameWindow>();
        foreach (var (hwnd, title) in FindGameWindows())
        {
            var parsed = TitleParser.Parse(title);
            if (parsed is not null) result.Add(new GameWindow(hwnd, title, parsed.CharacterName, parsed.ClassName));
        }
        return result;
    }

    /// <summary>Toutes les fenêtres visibles et titrées des clients, écran de connexion compris.</summary>
    public IReadOnlyList<(IntPtr Handle, string Title)> FindGameWindows()
    {
        var result = new List<(IntPtr, string)>();
        var isGameProcess = new Dictionary<uint, bool>();

        NativeMethods.EnumWindows((hwnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hwnd)) return true;

            NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
            if (!isGameProcess.TryGetValue(pid, out bool isGame))
            {
                isGame = IsGameProcess(pid);
                isGameProcess[pid] = isGame;
            }
            if (!isGame) return true;

            var title = NativeMethods.GetTitle(hwnd);
            if (title.Length > 0) result.Add((hwnd, title));
            return true;
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>Vrai si la fenêtre appartient à un client Dofus (même sur l'écran de connexion).</summary>
    public bool IsGameWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        return IsGameProcess(pid);
    }

    private bool IsGameProcess(uint pid)
    {
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return _processNames.Contains(process.ProcessName);
        }
        catch (Exception)
        {
            // Processus terminé entre-temps ou inaccessible.
            return false;
        }
    }

    private static HashSet<string> ToSet(IEnumerable<string> names) =>
        new(names.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);
}
