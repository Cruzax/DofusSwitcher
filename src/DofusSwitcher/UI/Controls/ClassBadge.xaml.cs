using System.Windows;
using System.Windows.Controls;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>Icône de perso : couleur de la classe + symbole (ou 2 lettres s'il n'y a pas d'image).</summary>
public partial class ClassBadge : UserControl
{
    public static readonly DependencyProperty AppearanceProperty = DependencyProperty.Register(
        nameof(Appearance), typeof(ClassAppearance), typeof(ClassBadge));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(ClassBadge), new PropertyMetadata(new CornerRadius(8)));

    public static readonly DependencyProperty ImageMarginProperty = DependencyProperty.Register(
        nameof(ImageMargin), typeof(Thickness), typeof(ClassBadge), new PropertyMetadata(new Thickness(5)));

    public ClassBadge() => InitializeComponent();

    public ClassAppearance? Appearance
    {
        get => (ClassAppearance?)GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Marge autour du symbole (pour laisser de la place à un badge par-dessus, par exemple).</summary>
    public Thickness ImageMargin
    {
        get => (Thickness)GetValue(ImageMarginProperty);
        set => SetValue(ImageMarginProperty, value);
    }
}
