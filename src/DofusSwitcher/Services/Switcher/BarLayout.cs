using System;
using System.Windows;
using System.Windows.Controls;
using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Dimensions de la barre flottante, dérivées de la config.</summary>
public sealed record BarLayout(Orientation Orientation, double SlotSize, Thickness ImageMargin, double HotkeyFontSize, CornerRadius Corner)
{
    public bool IsVertical => Orientation == Orientation.Vertical;

    public static BarLayout For(AppConfig config) => For(config.BarOrientation, config.BarSize);

    public static BarLayout For(BarOrientation barOrientation, BarSize barSize)
    {
        double size = barSize switch
        {
            BarSize.Small => 42,
            BarSize.Large => 72,
            _ => 56,
        };
        var orientation = barOrientation == BarOrientation.Vertical ? Orientation.Vertical : Orientation.Horizontal;
        // Le bas de l'image laisse la place à l'étiquette du raccourci.
        var imageMargin = new Thickness(size * 0.11, size * 0.07, size * 0.11, size * 0.23);
        return new BarLayout(orientation, size, imageMargin, size < 50 ? 9 : size > 64 ? 11 : 10, new CornerRadius(Math.Round(size / 6)));
    }
}
