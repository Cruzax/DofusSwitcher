using System.Collections.Generic;
using System.Collections.ObjectModel;
using DofusSwitcher.Core;

namespace DofusSwitcher.UI;

/// <summary>Vignettes de mosaïque : la vraie disposition, calculée avec l'équipe telle qu'elle est en ce moment.</summary>
public partial class SettingsWindow
{
    /// <summary>Aperçus des deux mosaïques, calculés comme la vraie (le 1er de l'équipe sert de perso actif).</summary>
    public ObservableCollection<PreviewTile> GridPreview { get; } = new();
    public ObservableCollection<PreviewTile> FocusPreview { get; } = new();

    /// <summary>Taille des vignettes (forme d'un écran 16:9), et sous quelle largeur une case passe en compact.</summary>
    public const double PreviewWidth = 248;
    public const double PreviewHeight = PreviewWidth * 9 / 16;
    private const double CompactBelow = 100;

    /// <summary>Les vignettes de la mosaïque montrent l'équipe telle qu'elle est (ordre, ajouts, retraits).</summary>
    private void UpdateMosaicPreview()
    {
        // Même calcul que la vraie mosaïque, sur un écran 1920×1080, puis réduit à la vignette.
        var screen = new TileRect(0, 0, 1920, 1080);
        Fill(GridPreview, MosaicLayout.Tiles(Characters.Count, screen));
        Fill(FocusPreview, MosaicLayout.Focus(Characters.Count, screen));

        void Fill(ObservableCollection<PreviewTile> preview, IReadOnlyList<TileRect> tiles)
        {
            double scale = PreviewWidth / screen.Width;
            preview.Clear();
            for (int i = 0; i < tiles.Count; i++)
            {
                var t = tiles[i];
                double width = t.Width * scale;
                preview.Add(new PreviewTile(Characters[i], t.X * scale, t.Y * scale, width, t.Height * scale,
                    Compact: width < CompactBelow, IsActive: i == 0));
            }
        }
    }
}
