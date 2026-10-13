using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Mosaïque : rangement des clients (grille ou perso actif en grand), plein écran sans bordure, transition quand
/// le perso actif change, étiquettes sur les fenêtres et barre des tâches cachée.
/// </summary>
public sealed partial class SwitcherController
{
    private readonly WindowAnimator _animator = new();
    private bool _isMosaic;
    private bool _isArranging;
    private List<IntPtr> _mosaicWindows = new();
    private TileRect _mosaicScreen;
    private IntPtr _mosaicFocus;

    /// <summary>Vrai quand les clients sont rangés en mosaïque (faux = chacun en plein écran sans bordure).</summary>
    public bool IsMosaic
    {
        get => _isMosaic;
        private set
        {
            if (!Set(ref _isMosaic, value)) return;
            UpdateTaskbar();
            UpdateMosaicLabels();
        }
    }

    /// <summary>Étiquettes (symbole + pseudo) à poser sur les clients ; liste vide = toutes cachées.</summary>
    public event Action<IReadOnlyList<MosaicLabel>>? MosaicLabelsChanged;

    /// <summary>
    /// Passe de la mosaïque au plein écran sans bordure et inversement. Les clients réglés sur "Plein écran"
    /// dans Dofus ne se laissent pas ranger : ils sont laissés tels quels et comptés dans le résultat.
    /// </summary>
    public async Task<MosaicResult> ToggleMosaicAsync()
    {
        // Un double clic rapide ne lance pas deux rangements qui se battraient.
        if (_isArranging) return new MosaicResult(0, 0);
        Refresh(); // pour reconnaître les persos d'un client lancé depuis le dernier scan
        var all = _scanner.FindGameWindows();
        var windows = all.Where(w => !WindowArranger.IsGameFullscreenMode(w.Handle)).Select(w => w.Handle).ToList();
        int skipped = all.Count - windows.Count;
        if (windows.Count == 0) return new MosaicResult(0, skipped);

        // Dans l'ordre de la barre (persos inconnus et écrans de connexion à la fin), pour que la mosaïque
        // soit toujours rangée pareil.
        var order = Config.Characters.Select(c => c.Name).ToList();
        int Rank(IntPtr hwnd)
        {
            var name = CharacterAt(hwnd);
            int index = name is null ? -1 : order.FindIndex(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
            return index < 0 ? int.MaxValue : index;
        }
        windows = windows.OrderBy(Rank).ToList();

        var foreground = NativeMethods.GetForegroundWindow();
        var active = windows.Contains(foreground) ? foreground : windows[0];
        var screen = WindowArranger.ScreenOf(active);
        bool toMosaic = !IsMosaic;
        _animator.Stop();
        _mosaicWindows = toMosaic ? windows : new List<IntPtr>();
        _mosaicScreen = screen;
        _mosaicFocus = active;
        var targets = toMosaic ? MosaicTargets() : windows.Select(hwnd => (hwnd, screen)).ToList();

        _isArranging = true;
        try
        {
            foreach (var (hwnd, tile) in targets) WindowArranger.PlaceBorderless(hwnd, tile);
            IsMosaic = toMosaic;

            // Dofus reprend sa résolution peu après le retrait des bords : on surveille et on replace.
            await KeepPlacedAsync(targets, TimeSpan.FromSeconds(1.5));

            // Dofus n'adopte vraiment sa nouvelle taille qu'en reprenant la main : chaque client passe un
            // instant au premier plan (comme un clic sur son icône), puis on revient au perso de départ.
            foreach (var (hwnd, _) in targets) WindowArranger.NotifyResized(hwnd);
            foreach (var (hwnd, _) in targets.Where(t => t.Item1 != active))
            {
                WindowActivator.Activate(hwnd);
                await Task.Delay(150);
            }
            WindowActivator.Activate(active);
            await KeepPlacedAsync(targets, TimeSpan.FromSeconds(0.5));
        }
        finally
        {
            _isArranging = false;
        }
        OnForegroundChanged(NativeMethods.GetForegroundWindow());
        UpdateMosaicLabels();
        return new MosaicResult(windows.Count, skipped);
    }

    private static async Task KeepPlacedAsync(IReadOnlyList<(IntPtr Handle, TileRect Tile)> targets, TimeSpan duration)
    {
        for (int elapsed = 0; elapsed < duration.TotalMilliseconds; elapsed += 100)
        {
            await Task.Delay(100);
            foreach (var (hwnd, tile) in targets)
            {
                if (!WindowArranger.IsPlaced(hwnd, tile)) WindowArranger.Move(hwnd, tile);
            }
        }
    }

    /// <summary>Case de chaque client : grille, ou perso actif en grand (selon les réglages).</summary>
    private List<(IntPtr, TileRect)> MosaicTargets()
    {
        var windows = _mosaicWindows.Where(NativeMethods.IsWindow).ToList();
        if (!Config.MosaicFocusActive)
        {
            var grid = MosaicLayout.Tiles(windows.Count, _mosaicScreen);
            return windows.Select((hwnd, i) => (hwnd, grid[i])).ToList();
        }

        // Le perso actif en premier (grande case), les autres dans l'ordre de la barre.
        if (windows.Remove(_mosaicFocus)) windows.Insert(0, _mosaicFocus);
        var tiles = MosaicLayout.Focus(windows.Count, _mosaicScreen);
        return windows.Select((hwnd, i) => (hwnd, tiles[i])).ToList();
    }

    /// <summary>En mosaïque "perso actif en grand", le client qui passe au premier plan prend la grande case.</summary>
    private void FollowFocus(IntPtr foregroundWindow)
    {
        if (!IsMosaic || _isArranging || !Config.MosaicFocusActive) return;
        if (foregroundWindow == _mosaicFocus || !_mosaicWindows.Contains(foregroundWindow)) return;
        _mosaicFocus = foregroundWindow;
        _animator.AnimateTo(MosaicTargets());
        UpdateMosaicLabels(); // cachées pendant la transition, reposées à l'arrivée (WindowAnimator.Finished)
    }

    /// <summary>
    /// Une étiquette par client en mosaïque, grande case comprise. Cachées hors mosaïque, pendant un rangement
    /// ou une transition, et quand une autre appli est au premier plan.
    /// </summary>
    private void UpdateMosaicLabels()
    {
        var labels = new List<MosaicLabel>();
        if (IsMosaic && _isGameInFront && !_isArranging && !_animator.IsRunning)
        {
            foreach (var (hwnd, tile) in MosaicTargets())
            {
                var name = CharacterAt(hwnd);
                var slot = Slots.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
                if (slot is not null) labels.Add(new MosaicLabel(slot, tile));
            }
        }
        MosaicLabelsChanged?.Invoke(labels);
    }

    /// <summary>
    /// Plus aucun client de la mosaïque ouvert (tout fermer, fermeture à la main…) : on en sort, sinon la barre
    /// des tâches resterait cachée et les étiquettes en place.
    /// </summary>
    private void EndMosaicIfClientsGone()
    {
        if (!IsMosaic || _isArranging || _mosaicWindows.Any(NativeMethods.IsWindow)) return;
        _animator.Stop();
        _mosaicWindows.Clear();
        IsMosaic = false;
    }

    /// <summary>
    /// En mosaïque, la barre des tâches passerait sur les clients du bas : on la cache tant qu'un client
    /// (ou l'appli) est au premier plan, et elle revient dès qu'on passe sur une autre appli.
    /// </summary>
    private void UpdateTaskbar()
    {
        if (IsMosaic && _isGameInFront) TaskbarHider.Hide();
        else TaskbarHider.Show();
    }
}
