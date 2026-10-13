using System;
using System.IO;
using DofusSwitcher.Core;
using Xunit;

namespace DofusSwitcher.Tests;

public sealed class ErrorLogTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "DofusSwitcherTests", Guid.NewGuid().ToString("N"));

    private string LogPath => Path.Combine(_dir, "errors.log");

    [Fact]
    public void Appends_context_and_exception()
    {
        var log = new ErrorLog(LogPath);
        log.Write("Premier", new InvalidOperationException("aïe"));
        log.Write("Second", new IOException("fichier bloqué"));

        var text = File.ReadAllText(LogPath);
        Assert.Contains("Premier", text);
        Assert.Contains("aïe", text);
        Assert.Contains("Second", text);
        Assert.Contains("fichier bloqué", text);
    }

    [Fact]
    public void Large_log_is_moved_aside()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(LogPath, new string('x', (int)ErrorLog.MaxBytes + 1));

        new ErrorLog(LogPath).Write("Nouveau", new Exception("récent"));

        Assert.True(File.Exists(LogPath + ".old"));
        Assert.True(new FileInfo(LogPath).Length < ErrorLog.MaxBytes);
        Assert.Contains("récent", File.ReadAllText(LogPath));
    }

    [Fact]
    public void Never_throws_when_path_is_unusable()
    {
        Directory.CreateDirectory(LogPath); // un dossier à la place du fichier
        new ErrorLog(LogPath).Write("Contexte", new Exception());
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}
