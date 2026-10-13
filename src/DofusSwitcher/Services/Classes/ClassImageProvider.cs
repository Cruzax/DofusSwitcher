using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Charge les images de classe déposées par l'utilisateur (iop.png, cra.jpg…).</summary>
public sealed class ClassImageProvider
{
    private static readonly string[] Extensions = { ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

    public static string DefaultFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DofusSwitcher", "classes");

    private readonly string _folder;
    private readonly Dictionary<string, (DateTime WrittenAt, ImageSource Image)> _cache = new();

    public ClassImageProvider(string folder) => _folder = folder;

    public string Folder => _folder;

    public ImageSource? Get(string? className)
    {
        if (string.IsNullOrWhiteSpace(className)) return null;
        var key = ClassStyle.ToFileKey(className);
        if (key.Length == 0) return null;

        var path = FindFile(_folder, key);
        if (path is null) return null;

        var writtenAt = File.GetLastWriteTimeUtc(path);
        if (_cache.TryGetValue(path, out var cached) && cached.WrittenAt == writtenAt) return cached.Image;

        var image = Load(path);
        if (image is not null) _cache[path] = (writtenAt, image);
        return image;
    }

    /// <summary>Chemin de l'image de la classe (toutes extensions acceptées), ou null.</summary>
    public static string? FindFile(string folder, string fileKey)
    {
        foreach (var extension in Extensions)
        {
            var path = Path.Combine(folder, fileKey + extension);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static ImageSource? Load(string path)
    {
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad; // ne garde pas le fichier verrouillé
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.DecodePixelWidth = 128;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            // Fichier illisible ou format non supporté : on retombe sur la pastille de couleur.
            return null;
        }
    }
}
