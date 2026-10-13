using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Fait glisser des fenêtres vers leur nouvelle case (environ 60 images/s). Un nouvel appel en cours de route
/// repart des positions actuelles : changer vite de perso ne fait pas sauter les fenêtres.
/// </summary>
public sealed class WindowAnimator
{
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(220);

    private readonly DispatcherTimer _frames = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(15) };
    private readonly DispatcherTimer _settle = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private readonly Stopwatch _clock = new();
    private List<Move> _moves = new();
    private int _settleChecks;

    private sealed record Move(IntPtr Handle, TileRect From, TileRect To);

    public WindowAnimator()
    {
        _frames.Tick += (_, _) => Frame();
        _settle.Tick += (_, _) => Settle();
    }

    public bool IsRunning => _frames.IsEnabled;

    /// <summary>Levé quand les fenêtres sont arrivées (ou tout de suite s'il n'y avait rien à déplacer).</summary>
    public event Action? Finished;

    public void AnimateTo(IReadOnlyList<(IntPtr Handle, TileRect Tile)> targets)
    {
        _settle.Stop();
        _moves = targets
            .Where(t => NativeMethods.IsWindow(t.Handle))
            .Select(t => new Move(t.Handle, CurrentRect(t.Handle) ?? t.Tile, t.Tile))
            .Where(m => m.From != m.To)
            .ToList();
        if (_moves.Count == 0)
        {
            _frames.Stop();
            Finished?.Invoke();
            return;
        }
        _clock.Restart();
        _frames.Start();
        Frame();
    }

    public void Stop()
    {
        _frames.Stop();
        _settle.Stop();
        _moves.Clear();
    }

    private void Frame()
    {
        double progress = _clock.Elapsed / Duration;
        foreach (var move in _moves)
        {
            var rect = MosaicLayout.Interpolate(move.From, move.To, progress);
            // Asynchrone : on n'attend pas que le jeu ait fini de se redessiner pour passer à l'image suivante.
            NativeMethods.SetWindowPos(move.Handle, IntPtr.Zero, rect.X, rect.Y, rect.Width, rect.Height,
                NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_ASYNCWINDOWPOS);
        }
        if (progress < 1) return;

        _frames.Stop();
        // Arrivée : comme un redimensionnement à la souris qu'on lâche, pour que le jeu adopte sa taille.
        foreach (var move in _moves)
        {
            WindowArranger.Move(move.Handle, move.To);
            if (move.From.Width != move.To.Width || move.From.Height != move.To.Height) WindowArranger.NotifyResized(move.Handle);
        }
        _settleChecks = 0;
        _settle.Start();
        Finished?.Invoke();
    }

    /// <summary>Dofus peut reprendre sa résolution juste après : on vérifie deux fois et on replace.</summary>
    private void Settle()
    {
        foreach (var move in _moves)
        {
            if (!WindowArranger.IsPlaced(move.Handle, move.To)) WindowArranger.Move(move.Handle, move.To);
        }
        if (++_settleChecks >= 2) _settle.Stop();
    }

    private static TileRect? CurrentRect(IntPtr hwnd) =>
        NativeMethods.GetWindowRect(hwnd, out var r) ? new TileRect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top) : null;
}
