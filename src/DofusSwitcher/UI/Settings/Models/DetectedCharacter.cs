using DofusSwitcher.Services;

namespace DofusSwitcher.UI;

/// <summary>Perso connecté en jeu et pas encore dans l'équipe (proposé à l'ajout).</summary>
public sealed record DetectedCharacter(string Name, string? ClassName, ClassAppearance Appearance);
