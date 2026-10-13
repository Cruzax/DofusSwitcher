using System;
using System.Windows.Interop;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>
/// Prévient quand une fenêtre clignote dans la barre des tâches (MP, invitation… quand le jeu le signale).
/// Lecture seule : on écoute les notifications du shell Windows, rien n'est envoyé au jeu.
/// </summary>
public sealed class FlashWatcher : IDisposable
{
    private const int WS_POPUP = unchecked((int)0x80000000);

    private readonly HwndSource _source;
    private readonly int _shellHookMessage;

    public event Action<IntPtr>? WindowFlashed;

    public FlashWatcher()
    {
        // Fenêtre invisible de premier niveau : les fenêtres "message-only" ne reçoivent pas les notifications du shell.
        _source = new HwndSource(new HwndSourceParameters("DofusSwitcherFlashWatcher") { WindowStyle = WS_POPUP, Width = 0, Height = 0 });
        _shellHookMessage = (int)NativeMethods.RegisterWindowMessage("SHELLHOOK");
        _source.AddHook(WndProc);
        NativeMethods.RegisterShellHookWindow(_source.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _shellHookMessage && wParam.ToInt64() == NativeMethods.HSHELL_FLASH) WindowFlashed?.Invoke(lParam);
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        NativeMethods.DeregisterShellHookWindow(_source.Handle);
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
