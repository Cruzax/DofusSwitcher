using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Interop;
using DofusSwitcher.Core;
using DofusSwitcher.Win32;

namespace DofusSwitcher.Services;

/// <summary>Raccourcis clavier globaux (actifs même quand Dofus est au premier plan).</summary>
public sealed class HotkeyManager : IDisposable
{
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly HwndSource _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 1;

    public HotkeyManager()
    {
        // Fenêtre invisible "message-only" qui reçoit les WM_HOTKEY.
        _source = new HwndSource(new HwndSourceParameters("DofusSwitcherHotkeys") { ParentWindow = HWND_MESSAGE, WindowStyle = 0 });
        _source.AddHook(WndProc);
    }

    /// <summary>Renvoie false si la combinaison est déjà prise (par une autre appli ou en double).</summary>
    public bool Register(Hotkey hotkey, Action action)
    {
        int id = _nextId++;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);
        if (vk == 0) return false;
        if (!NativeMethods.RegisterHotKey(_source.Handle, id, ToNative(hotkey.Modifiers) | NativeMethods.MOD_NOREPEAT, vk)) return false;
        _actions[id] = action;
        return true;
    }

    public void UnregisterAll()
    {
        foreach (var id in _actions.Keys) NativeMethods.UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
        // Windows n'accepte que les identifiants 1 à 0xBFFF : on repart de 1 à chaque réenregistrement (pause, réglages…).
        _nextId = 1;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var action))
        {
            action();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private static uint ToNative(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= NativeMethods.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= NativeMethods.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= NativeMethods.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= NativeMethods.MOD_WIN;
        return result;
    }

    public void Dispose()
    {
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }
}
