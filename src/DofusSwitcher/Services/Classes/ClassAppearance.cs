using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Ce qu'affiche une icône de perso : couleur de classe, image éventuelle, et 2 lettres à défaut d'image.</summary>
public sealed record ClassAppearance(Brush Background, ImageSource? Image, string Label)
{
    public bool ShowLabel => Image is null;
}

/// <summary>Construit l'apparence d'une classe et télécharge son symbole s'il manque.</summary>
public sealed class ClassAppearanceProvider
{
    private readonly ClassImageProvider _images;
    private readonly ClassImageDownloader _downloader;
    private readonly Dictionary<string, Brush> _brushes = new();

    public ClassAppearanceProvider(string imagesFolder)
    {
        _images = new ClassImageProvider(imagesFolder);
        _downloader = new ClassImageDownloader(imagesFolder);
    }

    public string ImagesFolder => _images.Folder;

    /// <summary>Levé quand une image vient d'être téléchargée.</summary>
    public event Action? ImagesChanged;

    public ClassAppearance Get(string? className, string fallbackName)
    {
        var image = _images.Get(className);
        if (image is null && className is not null) _ = DownloadAsync(className);

        // Les pseudos d'une team se ressemblent souvent : on préfère les lettres de la classe.
        var label = Abbreviate(className is null ? fallbackName : ClassStyle.ToFileKey(className));
        return new ClassAppearance(BrushFor(ClassStyle.ColorFor(className)), image, label);
    }

    private async Task DownloadAsync(string className)
    {
        if (await _downloader.EnsureImageAsync(className)) ImagesChanged?.Invoke();
    }

    private Brush BrushFor(string hex)
    {
        if (_brushes.TryGetValue(hex, out var brush)) return brush;
        brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        _brushes[hex] = brush;
        return brush;
    }

    private static string Abbreviate(string text) => (text.Length <= 2 ? text : text[..2]).ToUpperInvariant();
}
