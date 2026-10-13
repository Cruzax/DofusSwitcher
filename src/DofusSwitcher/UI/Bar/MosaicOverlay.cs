using System.Collections.Generic;
using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>Affiche une étiquette (symbole + pseudo) sur chaque client de la mosaïque, selon le contrôleur.</summary>
public sealed class MosaicOverlay
{
    private readonly List<TileLabel> _labels = new();

    public MosaicOverlay(SwitcherController controller) => controller.MosaicLabelsChanged += Show;

    private void Show(IReadOnlyList<MosaicLabel> labels)
    {
        // Les étiquettes sont réutilisées : rien n'est recréé à chaque changement de perso.
        while (_labels.Count < labels.Count) _labels.Add(new TileLabel());
        for (int i = 0; i < _labels.Count; i++)
        {
            if (i < labels.Count) _labels[i].Place(labels[i].Slot, labels[i].Tile);
            else _labels[i].Hide();
        }
    }
}
