using System.Windows.Input;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public class HotkeyTests
{
    [Theory]
    [InlineData("F1", ModifierKeys.None, Key.F1)]
    [InlineData("ctrl+f2", ModifierKeys.Control, Key.F2)]
    [InlineData("Ctrl + Shift + Tab", ModifierKeys.Control | ModifierKeys.Shift, Key.Tab)]
    [InlineData("Alt+1", ModifierKeys.Alt, Key.D1)]
    [InlineData("NumPad5", ModifierKeys.None, Key.NumPad5)]
    public void Parses_valid_hotkeys(string text, ModifierKeys modifiers, Key key)
    {
        Assert.True(Hotkey.TryParse(text, out var hotkey));
        Assert.Equal(new Hotkey(modifiers, key), hotkey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ctrl")]
    [InlineData("Ctrl+")]
    [InlineData("Foo")]
    [InlineData("F1+F2")]
    [InlineData("12")]
    [InlineData("LeftCtrl")]
    public void Rejects_invalid_hotkeys(string? text)
    {
        Assert.False(Hotkey.TryParse(text, out _));
    }

    [Theory]
    [InlineData("F1")]
    [InlineData("Ctrl+Alt+Shift+F12")]
    [InlineData("Alt+1")]
    [InlineData("Win+Tab")]
    public void Round_trips_through_text(string text)
    {
        Assert.True(Hotkey.TryParse(text, out var hotkey));
        Assert.Equal(text, hotkey.ToString());
    }
}
