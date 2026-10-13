using System;
using System.Threading.Tasks;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>Actions sur l'ensemble des clients Dofus ouverts.</summary>
public sealed partial class SwitcherController
{
    private bool _isClosingAll;

    /// <summary>Nombre de fenêtres de clients ouvertes (écran de connexion compris).</summary>
    public int CountGameWindows() => _scanner.FindGameWindows().Count;

    /// <summary>
    /// Demande à chaque client de se fermer, comme un clic sur sa croix (le jeu peut demander confirmation).
    /// Aucun processus n'est tué de force. Les clients sont fermés l'un après l'autre : fermés tous
    /// ensemble, ils libèrent leur mémoire en même temps et le PC gèle quelques secondes.
    /// </summary>
    public async Task CloseAllGameWindowsAsync()
    {
        if (_isClosingAll) return;
        _isClosingAll = true;
        try
        {
            foreach (var (hwnd, _) in _scanner.FindGameWindows())
            {
                if (!NativeMethods.PostMessage(hwnd, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero)) continue;
                // On attend que la fenêtre ait disparu, 5 s au plus (un client qui traîne ne bloque pas les autres).
                for (int waited = 0; waited < 5000 && NativeMethods.IsWindow(hwnd); waited += 100) await Task.Delay(100);
            }
        }
        finally
        {
            _isClosingAll = false;
        }
        Refresh();
    }
}
