using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DofusSwitcher.Core;

/// <summary>Nom de fichier image et couleur associés à chaque classe.</summary>
public static class ClassStyle
{
    public const string DefaultColor = "#2D2F36";

    private static readonly Dictionary<string, string> Colors = new()
    {
        ["cra"] = "#3F7D3A",
        ["ecaflip"] = "#B07A1E",
        ["eliotrope"] = "#2E8C8C",
        ["eniripsa"] = "#B5487A",
        ["enutrof"] = "#8A6A2E",
        ["feca"] = "#2F6FA8",
        ["forgelance"] = "#5B6E86",
        ["huppermage"] = "#6A4FA3",
        ["iop"] = "#B23A2E",
        ["osamodas"] = "#6E8F2A",
        ["ouginak"] = "#7A4B2A",
        ["pandawa"] = "#3E8E6E",
        ["roublard"] = "#4C4C5C",
        ["sacrieur"] = "#9E2B3C",
        ["sadida"] = "#4F8A2C",
        ["sram"] = "#5A3F73",
        ["steamer"] = "#2B7A9E",
        ["xelor"] = "#3A5BA0",
        ["zobal"] = "#8C5A9E",
    };

    /// <summary>"Xélor" → "xelor" : minuscules, sans accents ni caractères spéciaux.</summary>
    public static string ToFileKey(string className)
    {
        var builder = new StringBuilder();
        foreach (var c in className.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }

    public static string ColorFor(string? className) =>
        className is not null && Colors.TryGetValue(ToFileKey(className), out var color) ? color : DefaultColor;
}
