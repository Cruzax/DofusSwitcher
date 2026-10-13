using DofusSwitcher.Core;

namespace DofusSwitcher.Services;

/// <summary>Étiquette à poser sur un client de la mosaïque.</summary>
public sealed record MosaicLabel(CharacterSlot Slot, TileRect Tile);

/// <summary>Résultat d'un passage mosaïque / plein écran.</summary>
/// <param name="Arranged">Fenêtres rangées.</param>
/// <param name="SkippedFullscreen">Clients laissés tels quels car réglés sur "Plein écran" dans Dofus.</param>
public sealed record MosaicResult(int Arranged, int SkippedFullscreen);
