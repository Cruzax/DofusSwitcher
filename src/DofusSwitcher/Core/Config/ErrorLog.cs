using System;
using System.IO;

namespace DofusSwitcher.Core;

/// <summary>Journal des erreurs inattendues, pour comprendre un plantage après coup.</summary>
public sealed class ErrorLog
{
    /// <summary>Au-delà, l'ancien journal est renommé en .old : le fichier ne grossit pas indéfiniment.</summary>
    public const long MaxBytes = 256 * 1024;

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DofusSwitcher", "errors.log");

    public string FilePath { get; }

    public ErrorLog(string filePath) => FilePath = filePath;

    /// <summary>Ajoute une entrée datée. Ne lève jamais d'exception : le journal ne doit pas faire planter l'appli.</summary>
    public void Write(string context, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var file = new FileInfo(FilePath);
            if (file.Exists && file.Length > MaxBytes) File.Move(FilePath, FilePath + ".old", overwrite: true);
            File.AppendAllText(FilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
            // Disque plein, dossier inaccessible… : tant pis pour le journal.
        }
    }
}
