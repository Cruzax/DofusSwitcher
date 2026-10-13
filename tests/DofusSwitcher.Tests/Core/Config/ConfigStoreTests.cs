using System;
using System.IO;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DofusSwitcherTests", Guid.NewGuid().ToString("N"));

    private string ConfigPath => Path.Combine(_dir, "config.json");

    [Fact]
    public void Missing_file_gives_defaults()
    {
        var config = new ConfigStore(ConfigPath).Load();
        Assert.Empty(config.Characters);
        Assert.Equal("F6", config.NextHotkey);
        Assert.Null(config.PauseHotkey);
        Assert.Null(config.MosaicHotkey);
        Assert.True(config.AutoHideBar);
        Assert.True(config.MosaicFocusActive);
        Assert.Equal(BarSize.Normal, config.BarSize);
        Assert.Equal(new[] { "Dofus" }, config.ProcessNames);
    }

    [Fact]
    public void Saves_and_loads()
    {
        var store = new ConfigStore(ConfigPath);
        var config = new AppConfig
        {
            NextHotkey = "Ctrl+Tab",
            PauseHotkey = "Pause",
            MosaicHotkey = "Ctrl+M",
            BarLeft = 120,
            BarTop = 4,
            BarOrientation = BarOrientation.Vertical,
            BarSize = BarSize.Large,
            AutoHideBar = false,
        };
        config.Characters.Add(new CharacterConfig { Name = "Benji-Iop", Hotkey = "F1" });
        config.Characters.Add(new CharacterConfig { Name = "Soignator", Hotkey = null });

        store.Save(config);
        var loaded = store.Load();

        Assert.Equal(2, loaded.Characters.Count);
        Assert.Equal("Benji-Iop", loaded.Characters[0].Name);
        Assert.Equal("F1", loaded.Characters[0].Hotkey);
        Assert.Null(loaded.Characters[1].Hotkey);
        Assert.Equal("Ctrl+Tab", loaded.NextHotkey);
        Assert.Equal("Pause", loaded.PauseHotkey);
        Assert.Equal("Ctrl+M", loaded.MosaicHotkey);
        Assert.Equal(BarOrientation.Vertical, loaded.BarOrientation);
        Assert.Equal(BarSize.Large, loaded.BarSize);
        Assert.False(loaded.AutoHideBar);
        Assert.Contains("\"Vertical\"", File.ReadAllText(ConfigPath));
        Assert.Equal(120, loaded.BarLeft);
    }

    [Fact]
    public void Corrupt_file_gives_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(ConfigPath, "{ pas du json");
        var config = new ConfigStore(ConfigPath).Load();
        Assert.Empty(config.Characters);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
