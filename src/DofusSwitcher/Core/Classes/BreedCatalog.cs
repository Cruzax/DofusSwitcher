using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DofusSwitcher.Core;

/// <summary>Lit la réponse de https://api.dofusdb.fr/breeds : nom de classe → URL du symbole.</summary>
public static class BreedCatalog
{
    public const string BreedsUrl = "https://api.dofusdb.fr/breeds?$limit=50";

    /// <summary>Clé = <see cref="ClassStyle.ToFileKey"/> du nom dans chaque langue ("feca", "xelor"…).</summary>
    public static Dictionary<string, string> ParseSymbolUrls(string json)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return result;

        foreach (var breed in data.EnumerateArray())
        {
            if (!breed.TryGetProperty("img", out var img) || img.ValueKind != JsonValueKind.String) continue;
            var url = img.GetString();
            if (string.IsNullOrEmpty(url)) continue;
            if (!breed.TryGetProperty("shortName", out var names) || names.ValueKind != JsonValueKind.Object) continue;

            foreach (var name in names.EnumerateObject())
            {
                if (name.Name == "id" || name.Value.ValueKind != JsonValueKind.String) continue;
                var key = ClassStyle.ToFileKey(name.Value.GetString() ?? "");
                if (key.Length > 0) result.TryAdd(key, url);
            }
        }
        return result;
    }
}
