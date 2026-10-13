using System;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>Prévient dès que la fenêtre active change (doit être créé sur le thread UI).</summary>
public sealed class ForegroundWatcher : IDisposable
{
    // Gardé en champ pour que le GC ne collecte pas le délégué passé à Windows.
    private readonly NativeMethods.WinEventDelegate _callback;
    private readonly IntPtr _hook;

    public event Action<IntPtr>? ForegroundChanged;

    public ForegroundWatcher()
    {
        _callback = (_, _, hwnd, _, _, _, _) => ForegroundChanged?.Invoke(hwnd);
        _hook = NativeMethods.SetWinEventHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _callback, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) NativeMethods.UnhookWinEvent(_hook);
    }
}
