namespace DofusSwitcher.UI;

/// <summary>Case d'un aperçu de mosaïque, déjà réduite à la taille de la vignette.</summary>
/// <param name="Compact">Case trop petite pour la classe : symbole et pseudo seulement.</param>
/// <param name="IsActive">Le perso pris comme exemple de perso au premier plan (cerclé d'or).</param>
public sealed record PreviewTile(EditableCharacter Character, double Left, double Top, double Width, double Height,
    bool Compact, bool IsActive);
