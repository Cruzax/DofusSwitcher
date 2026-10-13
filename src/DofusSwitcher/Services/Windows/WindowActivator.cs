using System;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

public static class WindowActivator
{
    /// <summary>Met la fenêtre au premier plan (en la restaurant si elle est réduite).</summary>
    public static bool Activate(IntPtr hwnd)
    {
        if (!NativeMethods.IsWindow(hwnd)) return false;

        if (NativeMethods.IsIconic(hwnd)) NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);

        if (NativeMethods.SetForegroundWindow(hwnd) && NativeMethods.GetForegroundWindow() == hwnd) return true;

        // Windows refuse parfois SetForegroundWindow ; en se rattachant au thread de la fenêtre
        // active, on obtient le droit de changer le premier plan.
        var foreground = NativeMethods.GetForegroundWindow();
        uint foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, out _);
        uint currentThread = NativeMethods.GetCurrentThreadId();
        bool attached = foregroundThread != 0 && foregroundThread != currentThread
            && NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            NativeMethods.BringWindowToTop(hwnd);
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
            NativeMethods.SetForegroundWindow(hwnd);
        }
        finally
        {
            if (attached) NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
        }

        return NativeMethods.GetForegroundWindow() == hwnd;
    }
}
